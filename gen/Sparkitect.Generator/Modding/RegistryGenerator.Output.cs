using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    public static bool RenderRegistryRegistrationsUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName)
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = $"{unit.Model.TypeName}Registrations_{suffix}.g.cs";

        var ns = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? unit.Model.ContainingNamespace + ".Registrations"
            : settings.SgOutputNamespace + ".Registrations";

        var entries = unit.Entries
            .OrderBy(e => e.Id)
            .Select(e =>
            {
                var propName = ToPascalCase(e.Id);
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

        var model = new
        {
            Namespace = ns,
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
        fileName = $"{ToPascalCase(model.Key)}ID.g.cs";
        var tpl = new
        {
            CategoryPascal = ToPascalCase(model.Key)
        };
        return FluidHelper.TryRenderTemplate("Modding.RegistryIdContainer.Framework.liquid", tpl, out code);
    }

    public static bool RenderRegistryIdExtensionsFramework(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        var categoryPascal = ToPascalCase(model.Key);
        var modPascal = ToPascalCase(settings.ModId);
        var registrationsNs = settings.SgOutputNamespace;
        var extensionsNs = settings.SgOutputNamespace;

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

    public static bool RenderRegistryIdPropertiesUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName)
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = $"{unit.Model.TypeName}.IdProperties_{suffix}.g.cs";

        var categoryPascal = ToPascalCase(unit.Model.Key);
        var registrationsNs = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? unit.Model.ContainingNamespace + ".Registrations"
            : settings.SgOutputNamespace + ".Registrations";
        var extensionsNs = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? unit.Model.ContainingNamespace + ".IdExtensions"
            : settings.SgOutputNamespace + ".IdExtensions";

        var tpl = new
        {
            ExtensionsNamespace = extensionsNs,
            ModStructName = ToPascalCase(settings.ModId) + categoryPascal + "IDs",
            RegistrationsNamespace = registrationsNs,
            RegistryName = unit.Model.TypeName,
            SourceTag = suffix,
            Entries = unit.Entries.OrderBy(e => e.Id).Select(e => new { PropertyName = ToPascalCase(e.Id) }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryIdProperties.Unit.liquid", tpl, out code);
    }
    
    internal static bool RenderRegistryMetadata(RegistryModel model, out string code, out string fileName)
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
            Namespace = model.ContainingNamespace,
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

    internal static bool RenderRegistryConfigurator(ImmutableArray<RegistryModel> models, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = "RegistryConfigurator.g.cs";
        
        if (models.IsEmpty)
        {
            code = string.Empty;
            return false;
        }
        
        var configuratorModel = new
        {
            Namespace = settings.SgOutputNamespace,
            ConfiguratorClassName = "RegistryConfigurator",
            Registries = models.Select(m => new { 
                FactoryName = $"global::{m.ContainingNamespace}.{m.TypeName}_KeyedFactory"
            }).ToArray()
        };
        
        return FluidHelper.TryRenderTemplate("Modding.RegistryConfigurator.liquid", configuratorModel, out code);
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
        
        if (RenderRegistryMetadata(model, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryConfigurator(SourceProductionContext context, (ImmutableArray<RegistryModel> Left, ModBuildSettings Right) arg2)
    {
        var (models, settings) = arg2;
        
        if (RenderRegistryConfigurator(models, settings, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
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
