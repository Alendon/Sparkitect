using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    internal static void OutputRegistryMetadata(SourceProductionContext context, (RegistryModel model, ModBuildSettings setting) arg2)
    {
        var (model, settings) = arg2;
        
        var metadataModel = new
        {
            Namespace = model.ContainingNamespace,
            MetadataClassName = $"{model.TypeName}_Metadata",
            TypeName = model.TypeName,
            Key = model.Key,
            ContainingNamespace = model.ContainingNamespace
        };
        
        if (FluidHelper.TryRenderTemplate("Modding.RegistryMetadata.liquid", metadataModel, out var code))
        {
            var fileName = $"{model.TypeName}_Metadata.g.cs";
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryConfigurator(SourceProductionContext context, (ImmutableArray<RegistryModel> Left, ModBuildSettings Right) arg2)
    {
        var (models, settings) = arg2;
        
        if (models.IsEmpty) return;
        
        var configuratorModel = new
        {
            Namespace = settings.SgOutputNamespace,
            ConfiguratorClassName = "RegistryConfigurator",
            Registries = models.Select(m => new { 
                FactoryName = $"global::{m.ContainingNamespace}.{m.TypeName}_KeyedFactory"
            }).ToArray()
        };
        
        if (FluidHelper.TryRenderTemplate("Modding.RegistryConfigurator.liquid", configuratorModel, out var code))
        {
            var fileName = "RegistryConfigurator.g.cs";
            context.AddSource(fileName, code);
        }
    }
}