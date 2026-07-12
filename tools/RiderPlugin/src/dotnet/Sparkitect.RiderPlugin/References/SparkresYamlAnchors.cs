using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Shared detection of the two navigable anchors in a <c>.sparkres.yaml</c> file:
/// the top-level registry-method key and the entry-ID scalar nested under a sequence entry.
/// Reused by the reference factory, the signal daemon, and the reverse-index builder so all three
/// agree on which scalars participate.
/// </summary>
public static class SparkresYamlAnchors
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(SparkresYamlAnchors));

    /// <summary>The source text content of a scalar node.</summary>
    public static string? GetScalarText(IPlainScalarNode scalar) => scalar.GetText();

    /// <summary>
    /// True when the scalar is the key of a block-mapping entry at the document-root mapping level
    /// (the top-level <c>{registry-FQN}.{method}:</c> key) — i.e. not nested inside a sequence entry.
    /// </summary>
    public static bool IsTopLevelRegistryKey(IPlainScalarNode scalar)
    {
        var entry = BlockMappingEntryNavigator.GetByKey(scalar);
        return entry != null && GetEnclosingSequenceEntry(entry) == null;
    }

    /// <summary>
    /// True when the scalar is the key of a block-mapping entry that is a sequence element under a
    /// recognized top-level registry-method key; yields that registry's FQN.
    /// </summary>
    public static bool TryGetEntryIdAnchor(IPlainScalarNode scalar, out string? registryFqn)
    {
        registryFqn = null;

        var entry = BlockMappingEntryNavigator.GetByKey(scalar);
        if (entry == null)
            return false;

        var sequenceEntry = GetEnclosingSequenceEntry(entry);
        if (sequenceEntry == null)
            return false;

        var topKey = GetSequenceOwnerKey(sequenceEntry);
        if (topKey == null || !IsTopLevelRegistryKey(topKey))
            return false;

        registryFqn = GetScalarText(topKey);
        return !string.IsNullOrEmpty(registryFqn);
    }

    /// <summary>
    /// The owning mod's csproj <c>&lt;ModId&gt;</c> for a scalar's resource file — the reliable mod source,
    /// replacing the brittle project-name guess. Returns null when no <c>&lt;ModId&gt;</c> is set.
    /// </summary>
    public static string? GetModId(ITreeNode node) => SparkitectModId.Resolve(node);

    /// <summary>
    /// The registry's declared category for a YAML registration: resolves the registry type from its FQN
    /// (the top-level key minus the trailing <c>.method</c>) and reads the string-literal body of its
    /// <see cref="!:IRegistry.Identifier"/> property. Concrete type only; returns null when the literal is
    /// unreadable. Never the CLR short-name guess.
    /// </summary>
    public static string? GetRegistryCategory(ITreeNode node, string registryFqn)
    {
        var lastDot = registryFqn.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == registryFqn.Length - 1)
            return null;

        var registryClrName = registryFqn.Substring(0, lastDot);

        var module = node.GetPsiModule();
        var scope = module.GetPsiServices().Symbols
            .GetSymbolScope(module, withReferences: true, caseSensitive: true);
        var registryType = scope.GetTypeElementByCLRName(new ClrTypeName(registryClrName));
        if (registryType == null)
        {
            Logger.Trace($"GetRegistryCategory: registry type unresolved from CLR name '{registryClrName}' (FQN '{registryFqn}').");
            return null;
        }

        IProperty? identifierProperty = null;
        foreach (var property in registryType.Properties)
        {
            if (property.ShortName == "Identifier")
            {
                identifierProperty = property;
                break;
            }
        }

        if (identifierProperty == null)
        {
            Logger.Trace($"GetRegistryCategory: no static 'Identifier' property on registry type '{registryClrName}'.");
            return null;
        }

        var category = ReadPropertyLiteral(identifierProperty);
        if (category == null)
        {
            Logger.Trace($"GetRegistryCategory: 'Identifier' literal unreadable on registry type '{registryClrName}'.");
            return null;
        }

        Logger.Trace($"GetRegistryCategory: resolved category '{category}' for registry '{registryClrName}'.");
        return category;
    }

    /// <summary>
    /// Reads the first readable string-literal value from a property's source declaration(s) — covers the
    /// expression-bodied <c>=&gt; "..."</c> shape every registry's <c>Identifier</c> getter uses. Returns
    /// null when no readable string literal is present.
    /// </summary>
    private static string? ReadPropertyLiteral(IProperty property)
    {
        foreach (var declaration in property.GetDeclarations())
        foreach (var literal in declaration.Descendants<ICSharpLiteralExpression>().Collect())
        {
            if (literal.IsConstantValue() && literal.ConstantValue.IsString())
                return literal.ConstantValue.AsString();
        }

        return null;
    }

    private static ISequenceEntry? GetEnclosingSequenceEntry(ITreeNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current is ISequenceEntry sequenceEntry)
                return sequenceEntry;
            if (current is IYamlDocument)
                return null;
        }

        return null;
    }

    private static IPlainScalarNode? GetSequenceOwnerKey(ISequenceEntry sequenceEntry)
    {
        for (var current = sequenceEntry.Parent; current != null; current = current.Parent)
        {
            if (current is IBlockMappingEntry mappingEntry)
                return mappingEntry.Key as IPlainScalarNode;
            if (current is IYamlDocument)
                return null;
        }

        return null;
    }
}
