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
            TypeConstraint = string.Join(";", method.TypeConstraint)
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
            Kind: new ConfiguratorKind.Keyed("Sparkitect.Modding.IRegistryBase"),
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
    public void Configure(global::Sparkitect.DI.Container.IFactoryContainerBuilder<global::Sparkitect.Modding.IRegistryBase> builder, global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {{
        RegisterRegistries(builder, loadedMods);
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

    internal static void OutputIdPropertiesUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdPropertiesUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }
}
