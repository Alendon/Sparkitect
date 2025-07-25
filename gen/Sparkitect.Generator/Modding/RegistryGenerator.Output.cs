using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
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