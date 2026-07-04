using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding;

/// <summary>
/// Emits the group-first typed setting accessor hierarchy over <c>ISettingsManager</c>:
/// <c>settingsManager.&lt;Group&gt;.&lt;Setting&gt;</c> resolves to a <c>Setting&lt;T&gt;</c> handle by
/// delegating to the hand-written <c>GetSetting&lt;T&gt;</c> path. Group ownership is declared by
/// <c>[SettingGroup]</c> on a container struct (single-owner, fail-loud on a second ownership
/// declaration); each setting binds to its group via <c>[SettingAccessor]</c>. The emitted members are
/// pure sugar — the manual typed path works standalone without this generator.
/// </summary>
[Generator]
public class SettingsAccessorGenerator : IIncrementalGenerator
{
    private const string GroupAttribute = "Sparkitect.Settings.SettingGroupAttribute";
    private const string AccessorAttribute = "Sparkitect.Settings.SettingAccessorAttribute";

    // The settings registry category is "setting"; its generated id container is SettingID.
    private const string SettingCategoryPascal = "Setting";

    private static readonly DiagnosticDescriptor DuplicateGroupOwnership = new(
        id: "SPARK0270",
        title: "Duplicate settings group ownership",
        messageFormat:
        "Settings group '{0}' is already owned by '{1}'; '{2}' cannot re-declare ownership. Extend the existing group by adding accessor members with the same group id instead.",
        category: "Sparkitect",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        var groups = context.SyntaxProvider.ForAttributeWithMetadataName(
                GroupAttribute,
                static (node, _) => node is StructDeclarationSyntax,
                static (ctx, _) => ExtractGroup(ctx))
            .Where(static g => g is not null)
            .Select(static (g, _) => g!.Value)
            .Collect();

        var accessors = context.SyntaxProvider.ForAttributeWithMetadataName(
                AccessorAttribute,
                static (node, _) => node is PropertyDeclarationSyntax or MethodDeclarationSyntax,
                static (ctx, _) => ExtractAccessor(ctx))
            .Where(static a => a is not null)
            .Select(static (a, _) => a!.Value)
            .Collect();

        var combined = groups.Combine(accessors).Combine(buildSettings);
        context.RegisterSourceOutput(combined, Emit);
    }

    private static GroupInfo? ExtractGroup(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol structSymbol) return null;
        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null || attr.ConstructorArguments.Length < 1) return null;
        if (attr.ConstructorArguments[0].Value is not string groupId || string.IsNullOrEmpty(groupId)) return null;

        var ns = structSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : structSymbol.ContainingNamespace.ToDisplayString();
        var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
        return new GroupInfo(groupId, ns, structSymbol.Name, location);
    }

    private static AccessorInfo? ExtractAccessor(GeneratorAttributeSyntaxContext ctx)
    {
        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null || attr.ConstructorArguments.Length < 3) return null;
        if (attr.ConstructorArguments[0].Value is not string group || string.IsNullOrEmpty(group)) return null;
        if (attr.ConstructorArguments[1].Value is not string name || string.IsNullOrEmpty(name)) return null;
        if (attr.ConstructorArguments[2].Value is not string settingId || string.IsNullOrEmpty(settingId)) return null;

        ITypeSymbol? returnType = ctx.TargetSymbol switch
        {
            IPropertySymbol p => p.Type,
            IMethodSymbol m => m.ReturnType,
            _ => null
        };
        if (returnType is not INamedTypeSymbol { TypeArguments.Length: 1 } named || named.Name != "SettingDefinition")
            return null;

        var valueType = named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new AccessorInfo(group, name, settingId, valueType);
    }

    private void Emit(SourceProductionContext spc,
        ((ImmutableArray<GroupInfo> Groups, ImmutableArray<AccessorInfo> Accessors) Sets, ModBuildSettings Settings)
            input)
    {
        var (groups, accessors) = input.Sets;
        var settings = input.Settings;
        if (groups.IsDefaultOrEmpty) return;

        // Single-owner enforcement: first declaration wins; a second ownership declaration of the same
        // group id fails loud (and the conflicting accessor member is not emitted, avoiding a confusing
        // duplicate-member error on top of the diagnostic).
        var owners = new Dictionary<string, GroupInfo>();
        foreach (var g in groups)
        {
            if (owners.TryGetValue(g.GroupId, out var first))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DuplicateGroupOwnership, g.Location, g.GroupId, first.StructName, g.StructName));
                continue;
            }

            owners[g.GroupId] = g;
        }

        if (owners.Count == 0) return;

        var accessorsByGroup = accessors
            .Where(a => owners.ContainsKey(a.GroupId))
            .GroupBy(a => a.GroupId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var modIdPascal = StringCase.ToPascalCase(settings.ModId);

        var groupModels = owners.Values
            .OrderBy(g => g.GroupId, System.StringComparer.Ordinal)
            .Select(g => new
            {
                GroupPascal = StringCase.ToPascalCase(g.GroupId),
                StructNamespace = g.StructNamespace,
                StructName = g.StructName,
                StructFullName = string.IsNullOrEmpty(g.StructNamespace)
                    ? g.StructName
                    : g.StructNamespace + "." + g.StructName,
                Accessors = (accessorsByGroup.TryGetValue(g.GroupId, out var list) ? list : new List<AccessorInfo>())
                    .OrderBy(a => a.Name, System.StringComparer.Ordinal)
                    .Select(a => new
                    {
                        Name = a.Name,
                        ValueType = a.ValueTypeFullName,
                        IdProperty = StringCase.ToPascalCase(a.SettingId)
                    })
                    .ToList()
            })
            .ToList();

        var model = new
        {
            AccessorNamespace = settings.ComputeOutputNamespace(),
            IdExtensionsNamespace = settings.ComputeOutputNamespace("IdExtensions"),
            IdContainerPascal = SettingCategoryPascal,
            ModIdPascal = modIdPascal,
            Groups = groupModels
        };

        if (!FluidHelper.TryRenderTemplate("Modding.SettingsAccessor.Framework.liquid", model, out var code))
            return;

        spc.AddSource("SettingsAccessors.g.cs", code);
    }

    private readonly record struct GroupInfo(string GroupId, string StructNamespace, string StructName, Location Location);

    private readonly record struct AccessorInfo(string GroupId, string Name, string SettingId, string ValueTypeFullName);
}
