using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator;

public record ModBuildSettings(
    string ModName,
    string RootNamespace,
    bool EnableLogEnrichment,
    string SgOutputNamespace);

public static class GlobalOptionsExtensions
{
    public static IncrementalValueProvider<ModBuildSettings> GetModBuildSettings(this IncrementalGeneratorInitializationContext ctx)
    {
        return ctx.AnalyzerConfigOptionsProvider.Select((x, _) =>
        {
            var options = x.GlobalOptions;
            
            var logEnricherActive = options.TryGetValue("build_property.DisableLogEnrichmentGenerator", out var value) &&
                                    value.ToLowerInvariant() != "true";
            var modName = options.TryGetValue("build_property.ModName", out var modNameValue) ? modNameValue : string.Empty;
            var rootNamespace = options.TryGetValue("build_property.RootNamespace", out var rootNamespaceValue)
                ? rootNamespaceValue
                : string.Empty;
            var sgOutputNamespace = options.TryGetValue("build_property.SgOutputNamespace", out var sgOutputValue)
                ? sgOutputValue
                : string.Empty;
            
            return new ModBuildSettings(modName, rootNamespace, logEnricherActive, sgOutputNamespace);
        }).WithTrackingName("ModBuildSettings");
    }
}