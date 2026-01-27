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

        Dictionary<string, List<Dictionary<string, object>>>? root;
        try
        {
            root = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(raw);
        }
        catch
        {
            return;
        }

        if (root is null) return;

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

            foreach (var entryDict in entries)
            {
                // New format: each entry is a single-key dictionary where key=ID
                if (entryDict.Count != 1) continue;

                var kvp = entryDict.First();
                var id = kvp.Key;
                if (string.IsNullOrWhiteSpace(id)) continue;

                if (!set.Add(id))
                {
                    Report(ctx, RegistryDiagnostics.YamlDuplicateId, id, registryKey);
                }
            }
        }
    }

    private static void Report(AdditionalFileAnalysisContext ctx, DiagnosticDescriptor descriptor, params object[] args)
    {
        var location = Location.Create(ctx.AdditionalFile.Path, default, default);
        ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
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

            Dictionary<string, List<Dictionary<string, object>>>? root = null;
            try
            {
                root = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(raw);
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
                    continue;

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
                                "Sparkitect.Modding.UseResourceFileAttribute"
                                || a.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                                   "Sparkitect.Modding.UseResourceFileAttribute<TResource>")
                    .Select(a => new
                    {
                        Key = a.NamedArguments.FirstOrDefault(x => x.Key == "Key").Value.Value as string,
                        Required = a.NamedArguments.FirstOrDefault(x => x.Key == "Required").Value.Value as bool? ?? false,
                        Primary = a.NamedArguments.FirstOrDefault(x => x.Key == "Primary").Value.Value as bool? ?? false
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                    .ToArray();

                var declaredKeys = declared.Select(x => x.Key!).ToImmutableHashSet();
                var requiredKeys = declared.Where(x => x.Required).Select(x => x.Key!).ToImmutableHashSet();
                var primaryKey = declared.FirstOrDefault(x => x.Primary)?.Key
                                 ?? (declared.Length == 1 ? declared[0].Key : null);

                foreach (var entryDict in entries)
                {
                    if (entryDict.Count != 1) continue;
                    var kvp = entryDict.First();
                    var id = kvp.Key;

                    // Determine files from the value
                    var files = new Dictionary<string, string>();
                    var useSingleFile = false;

                    if (kvp.Value is string singleFile)
                    {
                        useSingleFile = true;
                        if (primaryKey is not null)
                        {
                            files[primaryKey] = singleFile;
                        }
                    }
                    else if (kvp.Value is Dictionary<object, object> multiFiles)
                    {
                        foreach (var f in multiFiles)
                        {
                            if (f.Key is string fileKey && f.Value is string fileName)
                            {
                                files[fileKey] = fileName;
                            }
                        }
                    }

                    // SPARK0243: unknown file key (only when using multi-file dictionary)
                    if (!useSingleFile && files.Count > 0)
                    {
                        foreach (var key in files.Keys)
                        {
                            if (!declaredKeys.Contains(key))
                            {
                                var valid = string.Join(", ", declaredKeys.OrderBy(s => s));
                                ReportFile(ctx, RegistryDiagnostics.YamlUnknownFileKey, file.Path, key, file.Path, registryType.Name, valid);
                            }
                        }
                    }

                    // SPARK0244: missing required file key
                    foreach (var req in requiredKeys)
                    {
                        var satisfied = files.ContainsKey(req);
                        if (!satisfied)
                        {
                            ReportFile(ctx, RegistryDiagnostics.YamlMissingRequiredFileKey, file.Path, id, file.Path, req);
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
