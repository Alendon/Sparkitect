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
        context.RegisterCompilationAction(AnalyzeYamlWithCompilation);
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

    private static void AnalyzeYamlWithCompilation(CompilationAnalysisContext ctx)
    {
        var additionalFiles = ctx.Options.AdditionalFiles.Where(f => f.Path.EndsWith(".sparkres.yaml")).ToArray();
        if (additionalFiles.Length == 0) return;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        foreach (var file in additionalFiles)
        {
            var text = file.GetText(ctx.CancellationToken);
            if (text is null) continue;
            var raw = text.ToString();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            Dictionary<string, List<ResourceYamlEntry>>? root = null;
            try
            {
                root = deserializer.Deserialize<Dictionary<string, List<ResourceYamlEntry>>>(raw);
            }
            catch
            {
                continue;
            }
            if (root is null) continue;

            foreach (var cur in root)
            {
                var registryKey = cur.Key;
                var entries = cur.Value;
                
                
                if (entries is null) continue;

                // Parse key: Namespace.Type.Method
                var idx = registryKey.LastIndexOf('.');
                if (idx <= 0 || idx >= registryKey.Length - 1)
                    continue; // malformed key; skip

                var fullTypeName = registryKey.Substring(0, idx);
                var methodName = registryKey.Substring(idx + 1);

                var registryType = ctx.Compilation.GetTypeByMetadataName(fullTypeName);
                if (registryType is null)
                {
                    ReportFile(ctx, RegistryDiagnostics.YamlUnknownRegistryKey, file.Path, registryKey);
                    continue;
                }

                // Ensure method exists and is marked with [RegistryMethod]
                var regMethod = registryType.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m =>
                    m.Name == methodName && m.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                        "Sparkitect.Modding.RegistryMethodAttribute"));
                if (regMethod is null)
                {
                    ReportFile(ctx, RegistryDiagnostics.YamlUnknownRegistryKey, file.Path, registryKey);
                    continue;
                }

                // Gather declared resource file identifiers for this registry
                var declared = registryType.GetAttributes()
                    .Where(a => a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                                "Sparkitect.Modding.UseResourceFileAttribute")
                    .Select(a => new
                    {
                        Identifier = a.NamedArguments.FirstOrDefault(x => x.Key == "Identifier").Value.Value as string,
                        Required = a.NamedArguments.FirstOrDefault(x => x.Key == "Required").Value.Value as bool? ?? false
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Identifier))
                    .ToArray();

                var declaredKeys = declared.Select(x => x.Identifier!).ToImmutableHashSet();
                var requiredKeys = declared.Where(x => x.Required).Select(x => x.Identifier!).ToImmutableHashSet();

                foreach (var e in entries)
                {
                    var files = e.Files ?? new Dictionary<string, string>();
                    var useSingleFile = !string.IsNullOrWhiteSpace(e.File);

                    // SPARK2043: unknown file key (only when using 'files' dictionary)
                    if (files.Count > 0)
                    {
                        foreach (var key in files.Keys)
                        {
                            if (!declaredKeys.Contains(key))
                            {
                                var valid = string.Join(", ", declaredKeys.OrderBy(s => s));
                                ReportFile(ctx, RegistryDiagnostics.YamlUnknownFileKey, file.Path, key, file.Path, registryType.Name + (string.IsNullOrEmpty(valid) ? string.Empty : ""), valid);
                            }
                        }
                    }

                    // SPARK2044: missing required file key
                    foreach (var req in requiredKeys)
                    {
                        var satisfied = files.ContainsKey(req) || (useSingleFile && declaredKeys.Count == 1 && declaredKeys.Contains(req));
                        if (!satisfied)
                        {
                            ReportFile(ctx, RegistryDiagnostics.YamlMissingRequiredFileKey, file.Path, e.Id ?? "<unknown>", file.Path, req);
                        }
                    }
                }
            }
        }
    }

    private static void ReportFile(CompilationAnalysisContext ctx, DiagnosticDescriptor descriptor, string path, params object[] args)
    {
        var location = Location.Create(path, default, default);
        ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }
}
