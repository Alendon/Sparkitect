using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.Explorer;

/// <summary>
/// The forward PSI walk backing the static Identification-structure explorer: for every solution mod
/// project it enumerates the generated <c>{Mod}{Category}IDs</c> structs under the well-known
/// <c>.CompilerGenerated.IdExtensions.</c> namespace (the convention codified by <see cref="RegistrationKey" />),
/// and each struct's leaf id <see cref="IProperty" />, into a per-mod category→entry structure. This is the
/// reverse of <c>DebugNavigation.ResolveLeaf</c> over the same substrate: navigation looks one leaf up by a
/// known (mod, category, item) triple; enumeration walks every leaf and derives its triple. The category is
/// read from structured metadata (<see cref="RegistrationKey.MarkerCategory" />), never by parsing type or
/// namespace names; alias leaves are not special-cased. All PSI access assumes a read lock is held.
/// </summary>
public static class ExplorerEnumeration
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(ExplorerEnumeration));

    // The generated IDs-struct namespace suffix + type-name suffix (the sanctioned convention from
    // RegistrationKey — used to LOCATE structs, never to derive a category).
    private const string IdExtensionsNamespaceSuffix = ".CompilerGenerated.IdExtensions";
    private const string IdsStructSuffix = "IDs";

    /// <summary>One leaf entry: its category (registry key) and the id member name that navigates it.</summary>
    public sealed class ExplorerEntryData
    {
        public ExplorerEntryData(string category, string item)
        {
            Category = category;
            Item = item;
        }

        public string Category { get; }
        public string Item { get; }
    }

    /// <summary>One mod project's flat entry list plus its display name.</summary>
    public sealed class ModExplorerData
    {
        public ModExplorerData(string modId, string displayName, IReadOnlyList<ExplorerEntryData> entries)
        {
            ModId = modId;
            DisplayName = displayName;
            Entries = entries;
        }

        public string ModId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ExplorerEntryData> Entries { get; }
    }

    /// <summary>
    /// Enumerates every solution mod project (a project carrying a <c>&lt;ModId&gt;</c>) and the generated
    /// Identification structure it declares. Never throws — an unreadable module degrades to no entries.
    /// </summary>
    public static IReadOnlyList<ModExplorerData> Enumerate(ISolution solution)
    {
        var result = new List<ModExplorerData>();
        var seen = new HashSet<string>();

        foreach (var module in solution.PsiModules().GetModules())
        {
            var project = (module as IProjectPsiModule)?.Project;
            if (project == null)
                continue;

            var modId = SparkitectModId.Resolve(project);
            if (string.IsNullOrEmpty(modId) || !seen.Add(modId!))
                continue;

            // "An unreadable module degrades to no entries" — a module-level fault (symbol scope, namespace
            // lookup) must still surface the mod in the selector with an empty tree, never abort the run.
            IReadOnlyList<ExplorerEntryData> entries;
            try
            {
                entries = EnumerateModule(module, modId!);
            }
            catch (System.Exception e)
            {
                Logger.Warn($"ExplorerEnumeration: module enumeration for mod '{modId}' threw, degrading to no entries: {e}");
                entries = System.Array.Empty<ExplorerEntryData>();
            }

            result.Add(new ModExplorerData(modId!, project.Name, entries));
        }

        return result.OrderBy(m => m.DisplayName).ToList();
    }

    private static IReadOnlyList<ExplorerEntryData> EnumerateModule(IPsiModule module, string modId)
    {
        var entries = new List<ExplorerEntryData>();

        // Scope to the mod's OWN symbols (no references): its generated IDs structs are compiled into its
        // own assembly, and each referenced mod is enumerated as its own project.
        var scope = module.GetPsiServices().Symbols
            .GetSymbolScope(module, withReferences: false, caseSensitive: true);

        var modPascal = RegistrationKey.SnakeToPascal(modId);
        var idExtensionsNs = scope.GetNamespace(modPascal + IdExtensionsNamespaceSuffix);
        if (idExtensionsNs == null)
        {
            Logger.Verbose($"ExplorerEnumeration: no IdExtensions namespace for mod '{modId}'.");
            return entries;
        }

        foreach (var idsStruct in idExtensionsNs.GetNestedTypeElements(scope))
        {
            if (!idsStruct.ShortName.EndsWith(IdsStructSuffix))
                continue;

            // Honor this walk's "never throws — degrades to no entries" contract at struct granularity: a
            // single struct whose PSI/attribute resolution faults (e.g. a build-261 constant-eval NRE) must
            // not abort the whole mod's enumeration and blank every mod in the window. Log loud, skip it.
            try
            {
                var structClrName = idsStruct.GetClrName().FullName;
                var category = ResolveStructCategory(idsStruct, structClrName, modId);
                if (string.IsNullOrEmpty(category))
                {
                    Logger.Verbose(
                        $"ExplorerEnumeration: no category resolved for struct '{structClrName}' (mod '{modId}').");
                    continue;
                }

                foreach (var leaf in idsStruct.Properties)
                    entries.Add(new ExplorerEntryData(category!, leaf.ShortName));
            }
            catch (System.Exception e)
            {
                Logger.Warn($"ExplorerEnumeration: enumeration of struct '{idsStruct.ShortName}' (mod '{modId}') threw, skipping: {e}");
            }
        }

        return entries;
    }

    /// <summary>
    /// Derives the single category of an IDs struct from structured metadata. Primary path: the leaf's
    /// <c>[RegisteredFrom]</c> owner carries the registration attribute, whose forward
    /// <see cref="RegistrationKey.MarkerCategory" /> gives the category — disambiguated by requiring that the
    /// category reconstructs THIS struct's CLR name (so a type registered into several registries maps each
    /// leaf to the right struct). Fallback for resource-only (YAML) structs with no C# registration attribute:
    /// the authoritative <c>IIdentificationManager.RegisterObject(mod, category, item)</c> literal the
    /// generator emits into the struct's own register method.
    /// </summary>
    private static string? ResolveStructCategory(ITypeElement idsStruct, string structClrName, string modId)
    {
        foreach (var leaf in idsStruct.Properties)
        {
            var category = CategoryViaMarker(leaf, structClrName, modId);
            if (!string.IsNullOrEmpty(category))
                return category;
        }

        foreach (var leaf in idsStruct.Properties)
        {
            var category = CategoryViaRegisterMethod(idsStruct, leaf.ShortName, modId);
            if (!string.IsNullOrEmpty(category))
                return category;
        }

        return null;
    }

    private static string? CategoryViaMarker(IProperty leaf, string structClrName, string modId)
    {
        var owner = RegisteredFromReader.Read(leaf);
        if (owner?.Type == null)
            return null;

        IAttributesOwner attributesOwner = owner.Type;
        if (owner.Member != null)
        {
            var member = owner.Type.GetMembers().FirstOrDefault(m => m.ShortName == owner.Member);
            if (member is IAttributesOwner memberOwner)
                attributesOwner = memberOwner;
        }

        foreach (var instance in attributesOwner.GetAttributeInstances(AttributesSource.Self))
        {
            var attributeType = instance.GetAttributeType().GetTypeElement();
            if (!RegistrationMarkerPredicate.IsRegistrationAttribute(attributeType))
                continue;

            var category = RegistrationKey.MarkerCategory(attributeType!);
            if (string.IsNullOrEmpty(category))
                continue;

            // Confirm this category is the one that reconstructs THIS struct (reuse the forward key
            // reconstruction — never parse the struct's name).
            var key = RegistrationKey.FromRuntimeIds(modId, category!, leaf.ShortName);
            if (key?.IdsStructClrName == structClrName)
                return category;
        }

        return null;
    }

    private static string? CategoryViaRegisterMethod(ITypeElement idsStruct, string memberName, string modId)
    {
        var providers = "Register_" + memberName + "_Providers";
        var resources = "Register_" + memberName + "_Resources";

        foreach (var member in idsStruct.GetMembers())
        {
            if (member is not IMethod || (member.ShortName != providers && member.ShortName != resources))
                continue;

            foreach (var declaration in member.GetDeclarations())
            {
                // The generated body opens with identificationManager.RegisterObject("mod","category","item")
                // as its first statement, so the first three string literals in document order are that
                // triple. Anchor on literal[0] == modId to guard against template drift.
                var literals = declaration.Descendants<ICSharpLiteralExpression>().Collect()
                    .Where(l => l.IsConstantValue() && l.ConstantValue.IsString())
                    .Select(l => l.ConstantValue.AsString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Take(3)
                    .ToList();

                if (literals.Count >= 2 && literals[0] == modId)
                    return literals[1];
            }
        }

        return null;
    }
}
