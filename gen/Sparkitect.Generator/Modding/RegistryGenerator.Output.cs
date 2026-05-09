using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    public static bool RenderRegistryRegistrationsUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName, string hintPrefix = "")
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = string.IsNullOrEmpty(hintPrefix)
            ? $"{unit.Model.TypeName}Registrations_{suffix}.g.cs"
            : $"{hintPrefix}_{unit.Model.TypeName}Registrations_{suffix}.g.cs";

        var ns = settings.ComputeOutputNamespace("Registrations");

        var entries = unit.Entries
            .OrderBy(e => e.Id)
            .Select(e =>
            {
                var propName = StringCase.ToPascalCase(e.Id);
                return new
                {
                    Id = e.Id,
                    PropertyName = propName,
                    Files = e.Files.OrderBy(f => f.fileId).Select(f => new { fileId = f.fileId, fileName = f.fileName }).ToArray(),
                    RegistrationCode = e.EmitRegistrationEntryCode("registry", propName)
                };
            })
            .ToArray();

        var useResourceManager = entries.Any(e => e.Files.Length > 0);

        var typePrefix = string.IsNullOrEmpty(hintPrefix) ? "" : hintPrefix + "_";

        var model = new
        {
            Namespace = ns,
            TypePrefix = typePrefix,
            RegistryName = unit.Model.TypeName,
            RegistryFullName = unit.Model.ContainingNamespace + "." + unit.Model.TypeName,
            CategoryKey = unit.Model.Key,
            ModId = settings.ModId,
            SourceTag = suffix,
            UseResourceManager = useResourceManager,
            Entries = entries
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryRegistrations.Unit.liquid", model, out code);
    }

    internal static bool RenderRegistryIdContainer(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{StringCase.ToPascalCase(model.Key)}ID.g.cs";
        var tpl = new
        {
            CategoryPascal = StringCase.ToPascalCase(model.Key)
        };
        return FluidHelper.TryRenderTemplate("Modding.RegistryIdContainer.Framework.liquid", tpl, out code);
    }

    public static bool RenderRegistryIdExtensionsFramework(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        var categoryPascal = StringCase.ToPascalCase(model.Key);
        var modPascal = StringCase.ToPascalCase(settings.ModId);
        var registrationsNs = settings.ComputeOutputNamespace("Registrations");
        var extensionsNs = settings.ComputeOutputNamespace();

        fileName = $"{model.TypeName}.IdFramework.g.cs";
        var tpl = new
        {
            ExtensionsNamespace = extensionsNs,
            CategoryPascal = categoryPascal,
            ModIdPascal = modPascal,
            ModStructName = modPascal + categoryPascal + "IDs",
            RegistrationsNamespace = registrationsNs,
            RegistryName = model.TypeName
        };
        return FluidHelper.TryRenderTemplate("Modding.RegistryIdExtensions.Framework.liquid", tpl, out code);
    }

    /// <summary>
    /// Renders one <c>partial class … : IHasIdentification</c> declaration per <see cref="TypeRegistrationEntry"/>
    /// in the unit. Each declaration lives in the concrete's own namespace and forwards its static
    /// <c>Identification</c> property to the matching <c>Registrations.{Registry}_{Source}.{ItemId}</c>
    /// static field already populated at boot via <c>IdentificationManager.RegisterObject(...)</c>.
    /// </summary>
    /// <remarks>
    /// Per D-04: filter is <see cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/>
    /// of <see cref="TypeRegistrationEntry"/> only — no <c>KeyedFactoryGeneration</c> filter. Auto-emit applies
    /// to ALL type-registered concretes, not only marker-flagged ones.
    /// </remarks>
    public static bool RenderAutoEmitIdentificationUnit(RegistrationUnit unit, ModBuildSettings settings,
        out string code, out string fileName)
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = $"{unit.Model.TypeName}.AutoEmitIdentification_{suffix}.g.cs";

        var typeEntries = unit.Entries
            .OfType<TypeRegistrationEntry>()
            .OrderBy(e => e.Id)
            .ToList();

        if (typeEntries.Count == 0)
        {
            code = string.Empty;
            return false;
        }

        var registrationsNs = settings.ComputeOutputNamespace("Registrations");

        var entries = typeEntries
            .Select(e =>
            {
                SplitTypeFullName(e.TypeFullName, out var ns, out var simpleName);
                return new
                {
                    TypeSimpleName = simpleName,
                    ContainingNamespace = ns,
                    TypeKindKeyword = e.TypeKind == RegistrationTypeKind.Struct ? "struct" : "class",
                    PropertyName = StringCase.ToPascalCase(e.Id)
                };
            })
            .ToArray();

        var tpl = new
        {
            RegistrationsNamespace = registrationsNs,
            TypePrefix = string.Empty,
            RegistryName = unit.Model.TypeName,
            SourceTag = suffix,
            Entries = entries
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryAutoEmitIdentification.Unit.liquid", tpl, out code);
    }

    /// <summary>
    /// Splits a fully-qualified type name (e.g. <c>"global::Ns.Sub.TypeName"</c>) into its containing
    /// namespace and simple type name. The leading <c>global::</c> prefix is stripped.
    /// </summary>
    private static void SplitTypeFullName(string typeFullName, out string containingNamespace,
        out string typeSimpleName)
    {
        var name = typeFullName.StartsWith("global::") ? typeFullName.Substring("global::".Length) : typeFullName;
        var lastDot = name.LastIndexOf('.');
        if (lastDot < 0)
        {
            containingNamespace = string.Empty;
            typeSimpleName = name;
            return;
        }

        containingNamespace = name.Substring(0, lastDot);
        typeSimpleName = name.Substring(lastDot + 1);
    }

    public static bool RenderRegistryIdPropertiesUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName, string hintPrefix = "")
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = string.IsNullOrEmpty(hintPrefix)
            ? $"{unit.Model.TypeName}.IdProperties_{suffix}.g.cs"
            : $"{hintPrefix}_{unit.Model.TypeName}.IdProperties_{suffix}.g.cs";

        var categoryPascal = StringCase.ToPascalCase(unit.Model.Key);
        var registrationsNs = settings.ComputeOutputNamespace("Registrations");
        var extensionsNs = settings.ComputeOutputNamespace("IdExtensions");

        var typePrefix = string.IsNullOrEmpty(hintPrefix) ? "" : hintPrefix + "_";

        var tpl = new
        {
            ExtensionsNamespace = extensionsNs,
            ModStructName = StringCase.ToPascalCase(settings.ModId) + categoryPascal + "IDs",
            RegistrationsNamespace = registrationsNs,
            TypePrefix = typePrefix,
            RegistryName = unit.Model.TypeName,
            SourceTag = suffix,
            Entries = unit.Entries.OrderBy(e => e.Id).Select(e => new { PropertyName = StringCase.ToPascalCase(e.Id) }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryIdProperties.Unit.liquid", tpl, out code);
    }
    
    internal static bool RenderRegistryMetadata(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}_Metadata.g.cs";
        
        var methodsMetadata = model.RegisterMethods.Select(method => new
        {
            FunctionName = method.FunctionName,
            PrimaryParameterKind = (int)method.PrimaryParameterKind,
            Constraint = (int)method.Constraint,
            TypeConstraint = string.Join(";", method.TypeConstraint),
            KeyedFactoryMarkerTBase = method.KeyedFactoryMarkerTBase ?? string.Empty
        }).ToArray();
        
        var registerMethodsString = string.Join(";", model.RegisterMethods.Select(m => m.FunctionName));
        
        var resourceFilesString = string.Join(";", model.ResourceFiles.Select(rf =>
            $"{rf.Key}:{(rf.Required ? 1 : 0)}:{(rf.Primary ? 1 : 0)}"));
        
        var metadataModel = new
        {
            Namespace = settings.ComputeOutputNamespace(),
            MetadataClassName = $"{model.TypeName}_Metadata",
            TypeName = model.TypeName,
            Key = model.Key,
            ContainingNamespace = model.ContainingNamespace,
            IsExternal = model.IsExternal ? "true" : "false",
            RegisterMethods = registerMethodsString,
            ResourceFiles = resourceFilesString,
            RegisterMethodsMetadata = methodsMetadata
        };
        
        return FluidHelper.TryRenderTemplate("Modding.RegistryMetadata.liquid", metadataModel, out code);
    }

    internal static bool RenderRegistryConfigurator(
        ImmutableArray<RegistryWithFactory> registriesWithFactories,
        ModBuildSettings settings,
        out string configuratorCode,
        out string configuratorFileName,
        out string shellCode,
        out string shellFileName)
    {
        shellFileName = "RegistryConfigurator_Shell.g.cs";
        shellCode = string.Empty;

        if (registriesWithFactories.IsEmpty)
        {
            configuratorCode = string.Empty;
            configuratorFileName = string.Empty;
            return false;
        }

        // Extract registrations from RegistryWithFactory wrappers
        var registrations = registriesWithFactories
            .Select(r => r.FactoryData.Registration)
            .ToImmutableValueArray();

        var options = new ConfiguratorOptions(
            ClassName: "RegistryConfigurator",
            Namespace: settings.ComputeOutputNamespace(),
            BaseType: "Sparkitect.DI.IRegistryConfigurator",
            EntrypointAttribute: "Sparkitect.DI.RegistryConfiguratorAttribute",
            Kind: new ConfiguratorKind.Keyed("string", "Sparkitect.Modding.IRegistryBase"),
            IsPartial: true,
            MethodName: "RegisterRegistries");

        // DI pipeline renders the registration method (partial class)
        var success = DiPipeline.RenderConfigurator(registrations, options,
            out configuratorCode, out configuratorFileName);

        // RegistryGenerator renders the partial class shell
        shellCode = GenerateConfiguratorShell(settings);

        return success;
    }

    private static string GenerateConfiguratorShell(ModBuildSettings settings)
    {
        return $@"#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {settings.ComputeOutputNamespace()};

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.RegistryConfigurator]
internal partial class RegistryConfigurator : global::Sparkitect.DI.IRegistryConfigurator
{{
    public void Configure(
        global::System.Collections.Generic.IDictionary<string, global::Sparkitect.DI.IKeyedFactory<global::Sparkitect.Modding.IRegistryBase>> registrations,
        global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {{
        RegisterRegistries(registrations, loadedMods);
    }}
}}";
    }


    internal static void OutputRegistryAttributes(SourceProductionContext context, RegistryModel model)
    {
        if (model.IsExternal) return;

        if (RenderRegistryAttributes(model, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryMetadata(SourceProductionContext context, (RegistryModel model, ModBuildSettings setting) arg2)
    {
        var (model, settings) = arg2;
        
        if (RenderRegistryMetadata(model, settings, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryFactory(SourceProductionContext context, (RegistryWithFactory Left, ModBuildSettings Right) pair)
    {
        var (rwf, settings) = pair;

        if (DiPipeline.RenderFactory(rwf.FactoryData.Factory, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }

        // Emit metadata entrypoint if facade metadata was extracted
        if (rwf.FacadeMetadata.Count > 0)
        {
            var factory = rwf.FactoryData.Factory;
            var wrapperTypeName = $"{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_KeyedFactory";

            var models = rwf.FacadeMetadata.Cast<IMetadataModel>().ToList();
            if (DiPipeline.RenderMetadataEntrypoint(wrapperTypeName, factory.ImplementationNamespace, models, settings,
                    out var metaCode, out var metaFileName))
                context.AddSource(metaFileName, metaCode);
        }
    }

    internal static void OutputRegistryConfigurator(SourceProductionContext context, (ImmutableArray<RegistryWithFactory> Left, ModBuildSettings Right) arg2)
    {
        var (registriesWithFactories, settings) = arg2;

        if (RenderRegistryConfigurator(registriesWithFactories, settings,
                out var configuratorCode, out var configuratorFileName,
                out var shellCode, out var shellFileName))
        {
            context.AddSource(configuratorFileName, configuratorCode);
            context.AddSource(shellFileName, shellCode);
        }
    }

    internal static void OutputRegistryIdFramework(SourceProductionContext context, (RegistryModel model, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdContainer(arg.model, arg.settings, out var code1, out var file1))
            context.AddSource(file1, code1);
        if (RenderRegistryIdExtensionsFramework(arg.model, arg.settings, out var code2, out var file2))
            context.AddSource(file2, code2);
    }

    internal static void OutputRegistryIdExtensions(SourceProductionContext context, (RegistryModel model, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdExtensionsFramework(arg.model, arg.settings, out var code, out var file))
            context.AddSource(file, code);
    }

    internal static void OutputRegistrationsUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderRegistryRegistrationsUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }

    // ── Task 2a: Branch A — string-driven configurator + shell per (registry × marker-flagged-method) ──

    public sealed record KeyedFactoryEmissionGroup(
        string ConfiguratorFileName, string ConfiguratorCode,
        string ShellFileName,        string ShellCode);

    /// <summary>
    /// For each marker-flagged method group in the unit, renders a configurator partial class
    /// (via DiPipeline.RenderConfigurator) and a shell class implementing
    /// IFactoryConfigurator&lt;Identification, TBase, …&gt;.
    /// Zero symbol access — pure string-driven.
    /// </summary>
    public static ImmutableArray<KeyedFactoryEmissionGroup> RenderTypeRegistrationKeyedFactory(
        RegistrationUnit unit, ModBuildSettings settings)
    {
        var result = ImmutableArray.CreateBuilder<KeyedFactoryEmissionGroup>();

        var markedEntries = unit.Entries
            .OfType<TypeRegistrationEntry>()
            .Where(e => e.KeyedFactoryGeneration is not null)
            .ToList();

        if (markedEntries.Count == 0)
            return result.ToImmutable();

        // Group by method name (Shape A — one configurator per registry × method pair)
        var grouped = markedEntries
            .GroupBy(e => e.MethodName)
            .ToList();

        foreach (var group in grouped)
        {
            var firstEntry = group.First();
            var configuratorClassName = firstEntry.KeyedFactoryGeneration!.ConfiguratorClassName;
            var tBaseFullName = firstEntry.KeyedFactoryGeneration!.TBaseFullName;
            var tBaseWithoutGlobal = tBaseFullName.StartsWith("global::")
                ? tBaseFullName.Substring("global::".Length)
                : tBaseFullName;

            // Build RegistrationModel instances for each entry in this group
            var registrations = new ImmutableValueArray<RegistrationModel>.Builder();
            foreach (var entry in group)
            {
                var typeFullName = entry.TypeFullName;
                // Derive namespace + simple name for factory type name
                var factoryTypeName = DeriveKeyedFactoryTypeName(typeFullName);
                var keyExpression =
                    $"global::Sparkitect.Modding.IdentificationHelper.Read<{typeFullName}>()";
                registrations.Add(new RegistrationModel(factoryTypeName, [], keyExpression));
            }

            var methodName = $"Register_{configuratorClassName}_Method";
            var entrypointAttributeName =
                $"{settings.ComputeOutputNamespace()}.{configuratorClassName}Attribute";

            var options = new ConfiguratorOptions(
                ClassName: configuratorClassName,
                Namespace: settings.ComputeOutputNamespace(),
                BaseType: tBaseWithoutGlobal,
                EntrypointAttribute: entrypointAttributeName,
                Kind: new ConfiguratorKind.Keyed("global::Sparkitect.Modding.Identification", tBaseWithoutGlobal),
                IsPartial: true,
                MethodName: methodName);

            if (!DiPipeline.RenderConfigurator(registrations.ToImmutableValueArray(), options,
                    out var configuratorCode, out var configuratorFileName))
                continue;

            var shellCode = GenerateKeyedFactoryConfiguratorShell(
                settings.ComputeOutputNamespace(), configuratorClassName, tBaseFullName);
            var shellFileName = $"{configuratorClassName}_Shell.g.cs";

            result.Add(new KeyedFactoryEmissionGroup(configuratorFileName, configuratorCode, shellFileName, shellCode));
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Derives the fully-qualified _KeyedFactory type name from a concrete type's full name.
    /// e.g. "global::DiTest.ClearColorPass" → "global::DiTest.ClearColorPass_KeyedFactory"
    /// </summary>
    private static string DeriveKeyedFactoryTypeName(string typeFullName)
    {
        // typeFullName is already "global::Ns.TypeName" shaped
        return $"{typeFullName}_KeyedFactory";
    }

    private static string GenerateKeyedFactoryConfiguratorShell(
        string namespaceName, string configuratorClassName, string tBaseFullName)
    {
        var attrName = $"{configuratorClassName}Attribute";
        return $@"#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {namespaceName};

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false)]
internal sealed class {attrName} : global::System.Attribute {{ }}

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[{attrName}]
internal partial class {configuratorClassName}
    : global::Sparkitect.DI.IFactoryConfigurator<global::Sparkitect.Modding.Identification, {tBaseFullName}, {attrName}>
{{
    public void Configure(
        global::System.Collections.Generic.IDictionary<global::Sparkitect.Modding.Identification, global::Sparkitect.DI.IKeyedFactory<{tBaseFullName}>> registrations,
        global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {{
        Register_{configuratorClassName}_Method(registrations, loadedMods);
    }}
}}";
    }

    internal static void OutputTypeRegistrationKeyedFactory(
        SourceProductionContext context,
        (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        foreach (var group in RenderTypeRegistrationKeyedFactory(arg.unit, arg.settings))
        {
            context.AddSource(group.ConfiguratorFileName, group.ConfiguratorCode);
            context.AddSource(group.ShellFileName, group.ShellCode);
        }
    }

    /// <summary>
    /// Branch B callback: emits one _KeyedFactory.g.cs per marker-flagged concrete type.
    /// The MarkerProviderConcrete carries the pre-computed FactoryModel (symbol-free, incremental-cacheable).
    /// </summary>
    internal static void OutputMarkerKeyedFactoryClass(
        SourceProductionContext context,
        (MarkerProviderConcrete concrete, ModBuildSettings settings) arg)
    {
        if (DiPipeline.RenderFactory(arg.concrete.Factory, out var code, out var fileName))
            context.AddSource(fileName, code);
    }

    internal static void OutputIdPropertiesUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdPropertiesUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }

    /// <summary>
    /// Output handler that emits one auto-emitted <c>IHasIdentification</c> partial-class file per
    /// <see cref="RegistrationUnit"/>. No-op when the unit contains no <see cref="TypeRegistrationEntry"/>
    /// (e.g. value/method/property providers).
    /// </summary>
    internal static void OutputAutoEmitIdentificationUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderAutoEmitIdentificationUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }
}
