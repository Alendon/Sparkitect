using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Shared detection of the two navigable anchors in a <c>.sparkres.yaml</c> file:
/// the top-level registry-method key and the entry-ID scalar nested under a sequence entry.
/// Reused by the reference factory, the signal daemon, and the reverse-index builder so all three
/// agree on which scalars participate.
/// </summary>
public static class SparkresYamlAnchors
{
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

    private const string RegistryAttributeFullName = "Sparkitect.Modding.RegistryAttribute";

    /// <summary>
    /// The owning mod's csproj <c>&lt;ModId&gt;</c> for a scalar's resource file — the reliable mod source
    /// (D-40), replacing the brittle project-name guess. Returns null when no <c>&lt;ModId&gt;</c> is set.
    /// </summary>
    public static string? GetModId(ITreeNode node) => SparkitectModId.Resolve(node);

    /// <summary>
    /// The registry's declared category for a YAML registration: resolves the registry type from its FQN
    /// (the top-level key minus the trailing <c>.method</c>) and reads its <c>[Registry(Identifier)]</c> —
    /// the same reliable category the C# path reads from the forward marker. Never the CLR short-name guess.
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
            return null;

        var instances = registryType.GetAttributeInstances(
            new ClrTypeName(RegistryAttributeFullName), AttributesSource.Self);
        foreach (var instance in instances)
        {
            var value = instance.NamedParameter("Identifier");
            if (!value.IsBadValue && value.IsConstant && value.ConstantValue.IsString())
                return value.ConstantValue.AsString();
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
