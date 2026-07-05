using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.Debug;

/// <summary>
/// Reverse lookup from a runtime string identification triple (mod, category, item) — carried on the
/// debug-channel wire — to the generated <c>{Mod}{Category}IDs.{Member}</c> leaf and thence to source.
/// Composes the shipped registration machinery unchanged: the string triple rebuilds the same
/// <see cref="RegistrationKey" /> a registration attribute would, the key resolves the leaf id property in
/// the module's symbol scope, and the leaf's <c>[RegisteredFrom]</c> owner edge yields both the
/// registration site (context menu, D-10) and the type declaration (double-click, D-10). The wire carries
/// strings, never the numeric Identification (runtime-assigned, un-mappable in the plugin).
/// </summary>
public static class DebugNavigation
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(DebugNavigation));

    /// <summary>
    /// Resolves a wire string triple to the generated leaf id property within <paramref name="module" />'s
    /// symbol scope, or null with a loud log on no match. No numeric fallback and no guessing — coverage is
    /// assumed, so an unresolved triple is surfaced loudly, never silently approximated.
    /// </summary>
    public static IProperty? ResolveLeaf(IPsiModule module, string modId, string category, string item)
    {
        var key = RegistrationKey.FromRuntimeIds(modId, category, item);
        if (key == null)
        {
            Logger.Warn(
                $"DebugNavigation: could not build a registration key from wire ids ('{modId}', '{category}', '{item}').");
            return null;
        }

        var scope = module.GetPsiServices().Symbols
            .GetSymbolScope(module, withReferences: true, caseSensitive: true);
        var idsStruct = scope.GetTypeElementByCLRName(new ClrTypeName(key.Value.IdsStructClrName));
        if (idsStruct == null)
        {
            Logger.Warn(
                $"DebugNavigation: generated IDs struct '{key.Value.IdsStructClrName}' not found for wire ids "
                + $"('{modId}', '{category}', '{item}').");
            return null;
        }

        var leaf = idsStruct.Properties.FirstOrDefault(p => p.ShortName == key.Value.MemberName);
        if (leaf == null)
            Logger.Warn(
                $"DebugNavigation: leaf '{key.Value.MemberName}' not found on '{key.Value.IdsStructClrName}' for "
                + $"wire ids ('{modId}', '{category}', '{item}').");

        return leaf;
    }

    /// <summary>
    /// The registration-site target (D-10 context menu): the id-string literal / resource entry-key anchor,
    /// resolved through the shared factory unchanged. Null when no registration anchors the leaf.
    /// </summary>
    public static DocumentRange? ResolveRegistrationSite(IProperty leaf) =>
        RegistrationFactory.FromLeaf(leaf)?.NavigableTarget;

    /// <summary>
    /// The type-declaration target (D-10 double-click): the <c>[RegisteredFrom]</c> owner type's declaration,
    /// read through the shared reader unchanged. Null for a resource-file owner (no C# type coordinate).
    /// </summary>
    public static DocumentRange? ResolveTypeDeclaration(IProperty leaf)
    {
        var owner = RegisteredFromReader.Read(leaf);
        if (owner?.Type == null)
            return null;

        // A member coordinate (stateless functions) targets the originating method itself; the bare
        // type is the target for type-level registrations (modules) and the fallback.
        if (owner.Member != null)
        {
            var memberDeclaration = owner.Type.GetMembers()
                .Where(m => m.ShortName == owner.Member)
                .SelectMany(m => m.GetDeclarations())
                .FirstOrDefault(d => d.GetSourceFile()?.Properties.IsGeneratedFile == false);
            if (memberDeclaration != null)
                return memberDeclaration.GetNameDocumentRange();
        }

        var declarations = owner.Type.GetDeclarations();
        if (declarations.Count == 0)
            return null;

        // Partial types: prefer the hand-written declaration — a generated partial (obj/) is not
        // navigable through the project model.
        var declaration =
            declarations.FirstOrDefault(d => d.GetSourceFile()?.Properties.IsGeneratedFile == false)
            ?? declarations[0];
        return declaration.GetNameDocumentRange();
    }
}
