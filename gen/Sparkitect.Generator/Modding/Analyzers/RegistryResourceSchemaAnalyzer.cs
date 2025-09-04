using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistryResourceSchemaAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        RegistryDiagnostics.YamlEntryMissingId,
        RegistryDiagnostics.YamlFileXorFiles,
        RegistryDiagnostics.YamlUnknownRegistryKey,
        RegistryDiagnostics.YamlUnknownFileKey,
        RegistryDiagnostics.YamlMissingRequiredFileKey,
        RegistryDiagnostics.YamlDuplicateId
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
    }

    private static void AnalyzeAdditionalFile(AdditionalFileAnalysisContext ctx)
    {
        var path = ctx.AdditionalFile.Path;
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".sparkres.yaml"))
            return;

        var text = ctx.AdditionalFile.GetText(ctx.CancellationToken);
        if (text is null) return;

        var raw = text.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Dictionary<string, List<ResourceYamlEntry>> root;
        try
        {
            root = deserializer.Deserialize<Dictionary<string, List<ResourceYamlEntry>>>(raw);
        }
        catch
        {
            // If YAML is malformed, ignore here — a separate diagnostic could handle parse errors later.
            return;
        }
        
        // Track duplicates per registry key
        var idSets = new Dictionary<string, HashSet<string>>();

        foreach (var cur in root)
        {
            var registryKey = cur.Key;
            var entries = cur.Value;
            
            if (entries is null) continue;
            if (!idSets.TryGetValue(registryKey, out var set))
            {
                set = new HashSet<string>();
                idSets[registryKey] = set;
            }

            foreach (var e in entries)
            {
                var id = e.Id ?? string.Empty;
                var file = e.File ?? string.Empty;
                var files = e.Files ?? new Dictionary<string, string>();

                if (string.IsNullOrWhiteSpace(id))
                {
                    Report(ctx, RegistryDiagnostics.YamlEntryMissingId, path);
                }

                var hasFile = !string.IsNullOrWhiteSpace(file);
                var hasFiles = files.Count > 0;
                if (hasFile && hasFiles)
                {
                    Report(ctx, RegistryDiagnostics.YamlFileXorFiles, idOrUnknown(id), path);
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    if (!set.Add(id))
                    {
                        // Duplicate within the same registry key (per file scope)
                        Report(ctx, RegistryDiagnostics.YamlDuplicateId, id, registryKey);
                    }
                }
            }
        }
    }

    private static void Report(AdditionalFileAnalysisContext ctx, DiagnosticDescriptor descriptor, params object[] args)
    {
        var location = Location.Create(ctx.AdditionalFile.Path, default, default);
        ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }

    private static string idOrUnknown(string id) => string.IsNullOrWhiteSpace(id) ? "<unknown>" : id;

    private sealed class ResourceYamlEntry
    {
        public string? Id { get; set; }
        public Dictionary<string, string>? Files { get; set; }
        public string? File { get; set; }
    }
}
