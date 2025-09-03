using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    
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
            $"{rf.identifier}:{(rf.optional ? 1 : 0)}"));
        
        var metadataModel = new
        {
            Namespace = model.ContainingNamespace,
            MetadataClassName = $"{model.TypeName}_Metadata",
            TypeName = model.TypeName,
            Key = model.Key,
            ContainingNamespace = model.ContainingNamespace,
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

    internal static bool RenderRegistryRegistrations(RegistryModel model, ImmutableArray<FileRegistrationEntry> yamlEntries, ImmutableArray<MethodRegistrationEntry> methodProviders, ImmutableArray<TypeRegistrationEntry> typeProviders, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}Registrations.g.cs";

        if (yamlEntries.IsDefaultOrEmpty && methodProviders.IsDefaultOrEmpty && typeProviders.IsDefaultOrEmpty)
        {
            code = string.Empty;
            return false;
        }

        var ns = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? model.ContainingNamespace
            : settings.SgOutputNamespace + ".Registrations";

        var resourceMethod = model.RegisterMethods.FirstOrDefault(m => m.PrimaryParameterKind == PrimaryParameterKind.None);

        var templateModel = new
        {
            Namespace = ns,
            RegistryName = model.TypeName,
            RegistryFullName = model.ContainingNamespace + "." + model.TypeName,
            CategoryKey = model.Key,
            ModNameSnakeCase = ToSnakeCase(settings.ModName),
            HasResourceMethod = resourceMethod is not null,
            ResourceMethodName = resourceMethod?.FunctionName ?? string.Empty,
            Entries = yamlEntries.Select(e => new
            {
                Id = e.Id,
                PropertyName = ToPascalCase(e.Id)
            }).ToArray(),
            MethodProviders = methodProviders.Select(mp => new
            {
                Id = mp.Id,
                PropertyName = ToPascalCase(mp.Id),
                RegistryMethodName = mp.RegistryMethodName,
                ProviderFullName = $"global::{mp.ProviderContainingType}.{mp.ProviderMethodName}",
                Parameters = mp.Parameters.Select((p, idx) => new { Type = p.paramType, IsNullable = p.isNullable, Var = $"p{idx}" }).ToArray()
            }).ToArray(),
            TypeProviders = typeProviders.Select(tp => new
            {
                Id = tp.Id,
                PropertyName = ToPascalCase(tp.Id),
                RegistryMethodName = tp.RegistryMethodName,
                TypeFullName = $"global::{tp.ProvidedTypeFullName}"
            }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryRegistrations.liquid", templateModel, out code);
    }

    internal static bool RenderRegistryIdContainer(RegistryModel model, out string code, out string fileName)
    {
        fileName = $"{ToPascalCase(model.Key)}ID.g.cs";

        var templateModel = new
        {
            CategoryPascal = ToPascalCase(model.Key)
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryIdContainer.liquid", templateModel, out code);
    }

    internal static bool RenderRegistryIdExtensions(RegistryModel model, ImmutableArray<FileRegistrationEntry> yamlEntries, ImmutableArray<MethodRegistrationEntry> methodProviders, ImmutableArray<TypeRegistrationEntry> typeProviders, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}.IdExtensions.g.cs";

        if (yamlEntries.IsDefaultOrEmpty && methodProviders.IsDefaultOrEmpty && typeProviders.IsDefaultOrEmpty)
        {
            code = string.Empty;
            return false;
        }

        var categoryPascal = ToPascalCase(model.Key);
        var modName = settings.ModName;
        var modStructName = modName + categoryPascal + "IDs";
        var registrationsNs = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? model.ContainingNamespace + ".Registrations"
            : settings.SgOutputNamespace + ".Registrations";
        var extensionsNs = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? model.ContainingNamespace + ".IdExtensions"
            : settings.SgOutputNamespace + ".IdExtensions";

        var templateModel = new
        {
            ExtensionsNamespace = extensionsNs,
            CategoryPascal = categoryPascal,
            ModName = modName,
            ModStructName = modStructName,
            RegistrationsNamespace = registrationsNs,
            RegistryName = model.TypeName,
            YamlEntries = yamlEntries.Select(e => new { PropertyName = ToPascalCase(e.Id) }).ToArray(),
            MethodEntries = methodProviders.Select(mp => new { PropertyName = ToPascalCase(mp.Id) }).ToArray(),
            TypeEntries = typeProviders.Select(tp => new { PropertyName = ToPascalCase(tp.Id) }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryIdExtensions.liquid", templateModel, out code);
    }

    
    internal static void OutputRegistryAttributes(SourceProductionContext context, RegistryModel model)
    {
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
}
