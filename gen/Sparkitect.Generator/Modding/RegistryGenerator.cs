using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Utilities;
using YamlDotNet.Core;

namespace Sparkitect.Generator.Modding;

[Generator]
public partial class RegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        var symbolRegistryWithFactoryProvider = context.SyntaxProvider.ForAttributeWithMetadataName(RegistryMarkerAttribute,
            (node, _) => node is ClassDeclarationSyntax, (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol symbol) return null;
                if (!symbol.AllInterfaces.Any(i =>
                        i.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryInterface)) return null;

                var registryAttribute = symbol.GetAttributes().FirstOrDefault(x =>
                    x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) is RegistryMarkerAttribute);
                if (registryAttribute is null) return null;

                var registryModel = ExtractModel(symbol, registryAttribute);
                if (registryModel is null) return null;

                // Extract factory data at the symbol boundary using DiPipeline
                var factory = DiPipeline.ExtractFactory(
                    symbol,
                    new FactoryIntent.Keyed(registryModel.Key),
                    RegistryBaseInterface);
                if (factory is null) return null;

                var registration = DiPipeline.ToRegistration(factory, symbol);
                var factoryData = new FactoryWithRegistration(factory, registration);

                // Extract facade metadata at the symbol boundary
                var facadeMetadata = DiPipeline.ExtractFacadeMetadata(symbol, "Sparkitect.Modding.RegistryFacadeAttribute")
                    .ToImmutableValueArray();

                return new RegistryWithFactory(registryModel, factoryData, facadeMetadata);
            }).NotNull();

        // Project RegistryModel from RegistryWithFactory for existing consumers
        var symbolRegistryModelsProvider = symbolRegistryWithFactoryProvider
            .Select((rwf, _) => rwf.Registry);

        var assemblyRegistryModelsProvider =
            context.CompilationProvider.Select((compilation, _) => ExtractModels(compilation));

        // Emit per-registry nested attributes early to stabilize attribute resolution in subsequent passes
        context.RegisterSourceOutput(symbolRegistryModelsProvider, OutputRegistryAttributes);

        // Emit the owning-module linkage partial for every registry (including External ones).
        context.RegisterSourceOutput(symbolRegistryModelsProvider, OutputRegistryOwningModule);

        context.RegisterSourceOutput(symbolRegistryModelsProvider.Combine(buildSettings), OutputRegistryMetadata);

        // Generate keyed factory class (and metadata entrypoint) for each registry via DiPipeline
        context.RegisterSourceOutput(symbolRegistryWithFactoryProvider.Combine(buildSettings), OutputRegistryFactory);

        // Generate configurator via DiPipeline (partial class with registration method)
        // and shell class with entrypoint attribute and interface implementation
        context.RegisterSourceOutput(symbolRegistryWithFactoryProvider.Collect().Combine(buildSettings),
            OutputRegistryConfigurator);


        /*
         * When looking at registrations with registry methods of the current compilation
         * The Attributes are not available at SG time (SG produces them)
         * This leads to the problem, that for compilation introduced RegistryMethods they might duplicate with other attributes
         * If this happens, aka the Generator reports that it is an Error Type and more than one type with this name exists,
         * Generating a partial class as a "dummy" fixes this issue, by introducing the target attribute class
         */

        var registryMapProvider = symbolRegistryModelsProvider.Collect().Combine(assemblyRegistryModelsProvider)
            .Select((x, _) => RegistryMap.Create(x));


        // Resource files: keep parse as a separate result carrying path + entries.
        // Combine with the per-file analyzer-config options so the absolute AdditionalText.Path
        // can be relativized against build_property.ProjectDir (emit a project-relative,
        // machine-move-surviving coordinate — never an interceptor-location token).
        var resourceFileRegistrationProvider = context.AdditionalTextsProvider
            .Where(x =>
                x.Path.EndsWith(ResourceFileSuffix))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, cancellation) =>
            {
                var (text, optionsProvider) = pair;
                var projectDir = optionsProvider.GetOptions(text)
                    .TryGetValue("build_property.ProjectDir", out var dir)
                    ? dir
                    : null;
                if (string.IsNullOrEmpty(projectDir) &&
                    optionsProvider.GlobalOptions.TryGetValue("build_property.ProjectDir", out var globalDir))
                {
                    projectDir = globalDir;
                }

                var relativePath = MakeProjectRelative(text.Path, projectDir);
                return (
                    text.Path,
                    Entries: ParseResourceYaml(text, relativePath, cancellation));
            });

        // Provider attribute usages: scan all attributes to capture provider candidates
        var providerCandidateProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (n, _) => n is AttributeSyntax,
                static (ctx, cancellationToken) =>
                    TryBuildProviderCandidate(ctx.Node, ctx.SemanticModel, cancellationToken))
            .NotNull();

        // Registration units from provider attributes (grouped per registry)
        var providerUnitsProvider = providerCandidateProvider
            .Combine(registryMapProvider)
            .Select(static (pair, _) => MapProviderCandidateToUnit(pair.Left, pair.Right))
            .NotNull()
            .Collect()
            .SelectMany(static (units, _) => GroupUnitsByRegistry(units, SourceKind.Provider, "Providers"));

        // Registration units from resource files (grouped per registry across all files)
        var resourceUnitsProvider = resourceFileRegistrationProvider
            .Combine(registryMapProvider)
            .Select(static (pair, _) => 
                BuildUnitsForResourceFile(pair.Left.Entries, pair.Right))
            .Collect()
            .SelectMany(static (allUnits, _) =>
            {
                // Flatten per-file units then group by registry
                var flat = new List<RegistrationUnit>();
                foreach (var units in allUnits)
                {
                    foreach (var u in units) flat.Add(u);
                }

                return GroupUnitsByRegistry([..flat], SourceKind.Yaml, "Resources");
            });

        // Outputs per registry: ID framework (always present)
        context.RegisterSourceOutput(symbolRegistryModelsProvider.Combine(buildSettings),
            static (spc, pair) => OutputRegistryIdFramework(spc, (pair.Left, pair.Right)));

        // ID extensions for external (assembly-based) registries
        context.RegisterSourceOutput(
            assemblyRegistryModelsProvider
                .SelectMany((models, _) => models)
                .Combine(buildSettings),
            static (spc, pair) => OutputRegistryIdExtensions(spc, (pair.Left, pair.Right)));

        // Outputs per unit: registrations + ID properties
        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputRegistrationsUnit(spc, (pair.Left, pair.Right)));
        context.RegisterSourceOutput(resourceUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputRegistrationsUnit(spc, (pair.Left, pair.Right)));

        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputIdPropertiesUnit(spc, (pair.Left, pair.Right)));
        context.RegisterSourceOutput(resourceUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputIdPropertiesUnit(spc, (pair.Left, pair.Right)));

        // Auto-emit the static `Identification` member as a partial for every TypeRegistrationEntry.
        // The type must declare `: IHasIdentification` in user source; this member binds to it.
        // Independent of the keyed-factory marker.
        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputAutoEmitIdentificationUnit(spc, (pair.Left, pair.Right)));
        context.RegisterSourceOutput(resourceUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputAutoEmitIdentificationUnit(spc, (pair.Left, pair.Right)));

        // Branch A — Configurator shell + matching attribute + C# 14 extension accessors
        // emitted ONCE in the registry's declaring assembly. Re-rooted onto
        // symbolRegistryWithFactoryProvider (same provider OutputRegistryConfigurator uses)
        // so the attribute Type exists in the declaring assembly — consumers can typeof() it
        // and the runtime no longer needs simple-name reflection.
        context.RegisterSourceOutput(symbolRegistryWithFactoryProvider.Combine(buildSettings),
            static (spc, pair) => OutputKeyedFactoryShellAndAccessors(spc, (pair.Left, pair.Right)));

        // Branch A (per-consumer split) — non-partial registrations class carrying the
        // now-public attribute, implementing IFactoryConfiguratorBase<TKey,TBase>.
        // Stays rooted on providerUnitsProvider because the registration *body* needs
        // the consumer's concretes.
        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputKeyedFactoryRegistrations(spc, (pair.Left, pair.Right)));

        // Branch B — Per-concrete _KeyedFactory.g.cs — driven from a parallel SyntaxProvider.
        // INamedTypeSymbol is resolved inside the lambda and does NOT cross the pipeline boundary;
        // only the string-typed MarkerProviderConcrete value flows through the pipeline.
        var markerConcreteProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (n, _) => n is AttributeSyntax,
                static (ctx, ct) => TryBuildMarkerProviderConcrete(ctx.Node, ctx.SemanticModel, ct))
            .NotNull()
            .Combine(registryMapProvider)
            .Select(static (pair, _) => ResolveMarkerConcrete(pair.Left, pair.Right))
            .NotNull();

        context.RegisterSourceOutput(markerConcreteProvider.Combine(buildSettings),
            static (spc, pair) => OutputMarkerKeyedFactoryClass(spc, (pair.Left, pair.Right)));
    }


    internal static ImmutableValueArray<FileRegistrationEntry> ParseResourceYaml(AdditionalText text,
        string? sourcePath, CancellationToken cancellation)
    {
        var result = new ImmutableValueArray<FileRegistrationEntry>.Builder();

        var sourceText = text.GetText(cancellation);
        if (sourceText is null) return result.ToImmutableValueArray();

        var raw = sourceText.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return result.ToImmutableValueArray();

        // Fixed shape: a top-level mapping of `FQN.Method:` keys, each to a sequence
        // of single-key mappings `- entry_id: file(s)`. The entry-id scalar IS the navigation target,
        // so we walk the low-level YamlDotNet Parser to capture its Mark (Start.Line / Start.Column).
        // High-level Deserialize discards position entirely; we deliberately avoid it for
        // the entry-id key. No SemanticModel, no InterceptsLocation/GetInterceptableLocation.
        try
        {
            using var reader = new global::System.IO.StringReader(raw);
            var parser = new YamlDotNet.Core.Parser(reader);

            parser.Consume<YamlDotNet.Core.Events.StreamStart>();
            if (!parser.TryConsume<YamlDotNet.Core.Events.DocumentStart>(out _))
                return result.ToImmutableValueArray();
            if (!parser.TryConsume<YamlDotNet.Core.Events.MappingStart>(out _))
                return result.ToImmutableValueArray();

            // Top-level mapping: key = "FQN.Method", value = sequence of entry mappings.
            while (!parser.Accept<YamlDotNet.Core.Events.MappingEnd>(out _))
            {
                var registryKey = parser.Consume<YamlDotNet.Core.Events.Scalar>().Value;

                if (!parser.TryConsume<YamlDotNet.Core.Events.SequenceStart>(out _))
                {
                    // Unexpected value shape for a registry key — skip it cleanly.
                    parser.SkipThisAndNestedEvents();
                    continue;
                }

                var methodSplitIndex = registryKey.LastIndexOf('.');
                var registryClass = methodSplitIndex > 0 ? registryKey.Substring(0, methodSplitIndex) : null;
                var methodName = methodSplitIndex > 0 ? registryKey.Substring(methodSplitIndex + 1) : null;

                while (!parser.Accept<YamlDotNet.Core.Events.SequenceEnd>(out _))
                {
                    ParseResourceEntry(parser, registryClass, methodName, sourcePath, result);
                }

                parser.Consume<YamlDotNet.Core.Events.SequenceEnd>();
            }
        }
        catch (YamlDotNet.Core.YamlException)
        {
            // Malformed YAML: degrade to no entries (same observable behavior as the old
            // Deserialize path returning an empty/failed parse).
            return result.ToImmutableValueArray();
        }

        return result.ToImmutableValueArray();
    }

    /// <summary>
    /// Parses one `- entry_id: file(s)` list item. Captures the entry-id scalar's source Mark
    /// (line/column, 1-based from YamlDotNet) so YAML-backed leaves carry a plain backward
    /// coordinate. Single-file (string) and multi-file (mapping) value shapes are preserved,
    /// as is the snake-case id guard. Non-matching shapes are skipped without throwing.
    /// </summary>
    private static void ParseResourceEntry(
        YamlDotNet.Core.IParser parser,
        string? registryClass,
        string? methodName,
        string? sourcePath,
        ImmutableValueArray<FileRegistrationEntry>.Builder result)
    {
        // Each entry is a single-key mapping: { entry_id: value }.
        if (!parser.TryConsume<YamlDotNet.Core.Events.MappingStart>(out _))
        {
            parser.SkipThisAndNestedEvents();
            return;
        }

        var idScalar = parser.Consume<YamlDotNet.Core.Events.Scalar>();
        var id = idScalar.Value;
        // Mark is 1-based for line/column in YamlDotNet; capture before validating so a real
        // entry always carries a non-zero line.
        var line = (int)idScalar.Start.Line;
        var column = (int)idScalar.Start.Column;

        var files = new ImmutableValueArray<(string fileId, string fileName)>.Builder();

        if (parser.Accept<YamlDotNet.Core.Events.Scalar>(out _))
        {
            // Single-file: value is the filename; special marker resolves the primary key later.
            // An empty scalar (`- entry1:` with no value) maps to no files, matching the prior
            // high-level path where a null value produced an empty Files set.
            var fileName = parser.Consume<YamlDotNet.Core.Events.Scalar>().Value;
            if (!string.IsNullOrEmpty(fileName))
            {
                files.Add((PrimaryFileMarker, fileName));
            }
        }
        else if (parser.TryConsume<YamlDotNet.Core.Events.MappingStart>(out _))
        {
            // Multi-file: value is a mapping of fileKey -> fileName.
            while (!parser.Accept<YamlDotNet.Core.Events.MappingEnd>(out _))
            {
                var fileKey = parser.Consume<YamlDotNet.Core.Events.Scalar>().Value;
                if (parser.Accept<YamlDotNet.Core.Events.Scalar>(out _))
                {
                    var fileName = parser.Consume<YamlDotNet.Core.Events.Scalar>().Value;
                    files.Add((fileKey, fileName));
                }
                else
                {
                    parser.SkipThisAndNestedEvents();
                }
            }

            parser.Consume<YamlDotNet.Core.Events.MappingEnd>();
        }
        else
        {
            // Unknown value shape — drop it.
            parser.SkipThisAndNestedEvents();
        }

        // Close the single-key entry mapping.
        parser.Consume<YamlDotNet.Core.Events.MappingEnd>();

        // Validation mirrors the previous high-level path: registry key must split into class.method,
        // and the id must be non-empty snake_case.
        if (registryClass is null || methodName is null) return;
        if (string.IsNullOrWhiteSpace(id) || !StringCase.IsSnakeCase(id)) return;

        var sortedFiles = files.OrderBy(f => f.fileId).ToImmutableValueArray();
        result.Add(new FileRegistrationEntry(registryClass, methodName, id, sortedFiles, sourcePath, line, column));
    }

    /// <summary>
    /// Relativizes an absolute resource-file path against the project directory so the emitted
    /// coordinate survives machine moves (regenerated locally). Falls back to the original
    /// path when no project dir is available or the path is already relative. Uses forward slashes
    /// for a stable, platform-neutral form in the generated attribute.
    /// </summary>
    internal static string? MakeProjectRelative(string? absolutePath, string? projectDir)
    {
        if (string.IsNullOrEmpty(absolutePath)) return absolutePath;
        if (string.IsNullOrEmpty(projectDir)) return Normalize(absolutePath!);

        var normalizedDir = projectDir!.Replace('\\', '/');
        if (!normalizedDir.EndsWith("/")) normalizedDir += "/";
        var normalizedPath = absolutePath!.Replace('\\', '/');

        if (normalizedPath.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath.Substring(normalizedDir.Length);
        }

        return normalizedPath;

        static string Normalize(string p) => p.Replace('\\', '/');
    }

    internal const string PrimaryFileMarker = "__primary__";

    internal static ImmutableValueArray<RegistrationUnit> BuildUnitsForResourceFile(
        ImmutableValueArray<FileRegistrationEntry> entries,
        RegistryMap regMap)
    {
        // Group by registry model from metadata class; avoid global Collect by local grouping only within this file
        var map =
            new Dictionary<string, (RegistryModel model, ImmutableValueArray<RegistrationEntry>.Builder builder)>();

        foreach (var e in entries)
        {
            if (!regMap.TryGetByFullName(e.RegistryClass, out var model) || model is null)
                continue;
            var key = model.ContainingNamespace + "." + model.TypeName;
            if (!map.TryGetValue(key, out var bucket))
            {
                bucket = (model, new ImmutableValueArray<RegistrationEntry>.Builder());
            }

            // Resolve PrimaryFileMarker to actual primary key
            var resolvedFiles = new ImmutableValueArray<(string fileId, string fileName)>.Builder();
            foreach (var file in e.Files)
            {
                if (file.fileId == PrimaryFileMarker)
                {
                    // Find primary key: explicit Primary=true, or single slot
                    var primaryKey = model.ResourceFiles.FirstOrDefault(rf => rf.Primary).Key
                                     ?? (model.ResourceFiles.Count == 1 ? model.ResourceFiles.First().Key : null);
                    if (primaryKey is not null)
                    {
                        resolvedFiles.Add((primaryKey, file.fileName));
                    }
                }
                else
                {
                    resolvedFiles.Add(file);
                }
            }

            var files = resolvedFiles.OrderBy(f => f.fileId).ToImmutableValueArray();
            // Thread the plain YAML backward coordinate (path + line/column captured at parse time)
            // onto the resource entry so the IdProperties projection can surface it.
            var entry = new ResourceRegistrationEntry(e.Id, files, e.MethodName,
                e.SourcePath, e.SourceLine, e.SourceColumn);
            bucket.builder.Add(entry);
            map[key] = bucket;
        }

        // Freeze per registry into units
        var units = new ImmutableValueArray<RegistrationUnit>.Builder();
        foreach (var kvp in map)
        {
            var sortedEntries = kvp.Value.builder
                .OrderBy(x => x.Id)
                .ToImmutableValueArray();

            units.Add(new RegistrationUnit(
                kvp.Value.model,
                SourceKind.Yaml,
                "Resources",
                sortedEntries));
        }

        return units.ToImmutableValueArray();
    }

    internal static ImmutableArray<RegistrationUnit> GroupUnitsByRegistry(ImmutableArray<RegistrationUnit> units,
        SourceKind kind, string sourceTag)
    {
        var map = new Dictionary<RegistryModel, ImmutableValueArray<RegistrationEntry>.Builder>();

        foreach (var unit in units)
        {
            if (unit.SourceKind != kind) continue;
            if (!map.TryGetValue(unit.Model, out var builder))
            {
                builder = new ImmutableValueArray<RegistrationEntry>.Builder();
                map[unit.Model] = builder;
            }

            foreach (var e in unit.Entries)
            {
                // Files are already sorted at creation time
                builder.Add(e);
            }
        }

        var result = new List<RegistrationUnit>();
        foreach (var kv in map)
        {
            var orderedEntries = kv.Value
                .OrderBy(x => x.Id)
                .ToImmutableValueArray();

            result.Add(new RegistrationUnit(kv.Key, kind, sourceTag, orderedEntries));
        }

        return [..result];
    }


    internal static RegistryModel? ExtractModel(INamedTypeSymbol symbol, AttributeData registryAttribute)
    {
        var identifierEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key is RegistryAttributeIdField);
        if (identifierEntry.Value.Value is not string id || string.IsNullOrWhiteSpace(id)) return null;
        if (!StringCase.IsSnakeCase(id)) return null;

        var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(namespaceName) ||
            symbol.ContainingNamespace?.IsGlobalNamespace is true) return null;

        var externalEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key == "External");
        var isExternal = externalEntry.Value.Value is true;

        // D-06: optional registry-level alias suffix, applied to every alias THIS registry emits into
        // other registries' id-space (Plan 04). Null when the mod author declares no suffix.
        var aliasSuffixEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key == "AliasSuffix");
        var aliasSuffix = aliasSuffixEntry.Value.Value as string;

        //TODO Registry Analyzer: Registry class cannot live outside namespace
        //General Analyzer (Utility class): No Type outside defined root namespace
        //Alternative: Define "GeneratorBaseNamespace", where the generator places it general entries
        return new RegistryModel(symbol.Name, id, namespaceName!, isExternal, ExtractRegisterMethods(symbol),
            ExtractResourceFiles(symbol), OwningModuleFullName: ExtractOwningModule(symbol), AliasSuffix: aliasSuffix);
    }

    /// <summary>
    /// Reads the owning-module type from the constructed <c>IRegistry&lt;TModule&gt;</c> implemented by the
    /// registry class. Detection is unchanged: the class matches because <c>IRegistry&lt;TModule&gt; : IRegistry</c>,
    /// so the constructed interface is present in <see cref="INamedTypeSymbol.AllInterfaces"/> with a single
    /// type argument (the bare non-generic <c>IRegistry</c> has zero). Returns null when a registry still
    /// implements only the bare <c>IRegistry</c>.
    /// </summary>
    internal static string? ExtractOwningModule(INamedTypeSymbol symbol)
    {
        var constructed = symbol.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryInterface
            && i.TypeArguments.Length == 1);
        if (constructed is null) return null;

        return constructed.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static ImmutableValueArray<(string Key, bool Required, bool Primary)> ExtractResourceFiles(
        INamedTypeSymbol symbol)
    {
        var result = new ImmutableValueArray<(string Key, bool Required, bool Primary)>.Builder();

        var attributes = symbol.GetAttributes().Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) is UseResourceFileAttribute
            || x.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) is "Sparkitect.Modding.UseResourceFileAttribute<TResource>");

        foreach (var attributeData in attributes)
        {
            var key = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Key")
                .Value.Value as string;

            var required = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Required")
                .Value.Value;

            var primary = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Primary")
                .Value.Value;

            if (key is null) continue;

            result.Add((key, required is true, primary is true));
        }

        return result.ToImmutableValueArray();
    }

    internal static ImmutableValueArray<RegisterMethodModel> ExtractRegisterMethods(INamedTypeSymbol symbol)
    {
        var result = new ImmutableValueArray<RegisterMethodModel>.Builder();

        var candidates = symbol.GetMembers().OfType<IMethodSymbol>()
            .Select((IMethodSymbol x, AttributeData attribute)? (x) =>
            {
                var attribute = x.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                    RegistryMethodMarkerAttribute);
                if (attribute is null) return null;
                return (x, attribute);
            }).OfType<(IMethodSymbol x, AttributeData attribute)>();

        foreach (var (method, _) in candidates)
        {
            // Parameter-count guard stays; the type-parameter-count cap is lifted (D-02) — a register
            // method (value OR type source) may carry 0..N type parameters, resolved per-registration
            // from its source.
            if (method.Parameters.Length == 0 || method.Parameters.Length > 2) continue;

            if (method.Parameters.First().Type.ToDisplayString(DisplayFormats.NamespaceAndType) !=
                IdentificationStruct) continue;

            var markerTBase = ExtractKeyedFactoryMarkerTBase(method);
            // TKey = the marker-flagged method's first parameter type (e.g. `Identification`).
            // Captured even when the marker is absent so the model is symmetric with TBase; only
            // emission paths that gate on markerTBase actually consume the TKey field.
            var markerTKey = method.Parameters.First().Type
                .ToDisplayString(DisplayFormats.NamespaceAndType);

            // D-04/D-05: which type parameter (if any) opts into same-registry Identification<T> emission,
            // plus 0..N cross-registry [TypedIdentification<TTarget>] linkages (D-08 kind-discriminated walk).
            var typedIdentificationExtraction = ExtractTypedIdentificationMarkers(method);
            var typedIdentificationTypeParameterName = typedIdentificationExtraction.BareMarker;
            var crossRegistryMarkers = typedIdentificationExtraction.CrossMarkers;

            //Method registration
            if (method.Parameters.Length == 2)
            {
                var parameter = method.Parameters.ElementAt(1);

                if (method.TypeParameters.Length > 0)
                {
                    // The value parameter must reference ANY of the method's type parameters: either a
                    // bare type parameter `T` (RegisterComponent<T>(Identification, T)) or a constructed
                    // generic that mentions one among its type arguments (RegisterSetting<T>(Identification,
                    // SettingDefinition<T>)). The wrapper case keeps the closed generic value type intact
                    // through the Registrations<> machinery — C# infers every T from the provider return
                    // type. A 2+-type-parameter value method (RegisterX<T1,T2>(Identification,
                    // Wrapper<T1,T2>)) captures its generics here instead of falling through to plain Value.
                    if (!method.TypeParameters.Any(tp => MentionsTypeParameter(parameter.Type, tp))) continue;

                    // Anchor constraints (flat Constraint/TypeConstraint) stay on TypeParameters.First()
                    // for the KeyedFactory path; the full per-type-parameter capture below is additive.
                    ParseTypeParameterConstraints(method.TypeParameters.First(), out var constraintFlag,
                        out var typeConstraints);
                    CaptureTypeParameterResolutionInputs(method.TypeParameters, out var typeParameterNames,
                        out var constraintRefs);
                    var valueParameterGeneric = BuildValueParameterGeneric(parameter.Type, method.TypeParameters);

                    result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.Value, constraintFlag,
                        typeConstraints, markerTBase, markerTKey, typedIdentificationTypeParameterName,
                        TypeParameterNames: typeParameterNames, ConstraintRefs: constraintRefs,
                        ValueParameterGeneric: valueParameterGeneric,
                        CrossRegistryMarkers: crossRegistryMarkers));

                    continue;
                }

                result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.Value, TypeConstraintFlag.None,
                    ((string[])[parameter.ToDisplayString(DisplayFormats.NamespaceAndType)]).ToImmutableValueArray(),
                    markerTBase, markerTKey, typedIdentificationTypeParameterName,
                    CrossRegistryMarkers: crossRegistryMarkers));

                continue;
            }

            //Type registration
            if (method.TypeParameters.Length > 0)
            {
                // Lifted from == 1 → > 0 (D-02): a multi-type-parameter, single-Identification-parameter
                // method (Reg<T1,T2>(Identification) where T1 : RelationShip<T2>) classifies as Type
                // instead of silently falling through to the None/resource-file branch below.
                ParseTypeParameterConstraints(method.TypeParameters.First(), out var constraintFlag,
                    out var typeConstraints);
                CaptureTypeParameterResolutionInputs(method.TypeParameters, out var typeParameterNames,
                    out var constraintRefs);
                result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.Type, constraintFlag,
                    typeConstraints, markerTBase, markerTKey, typedIdentificationTypeParameterName,
                    TypeParameterNames: typeParameterNames, ConstraintRefs: constraintRefs,
                    CrossRegistryMarkers: crossRegistryMarkers));

                continue;
            }


            //Resource file registration
            result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.None, TypeConstraintFlag.None, [],
                markerTBase, markerTKey, CrossRegistryMarkers: crossRegistryMarkers));
        }

        return result.ToImmutableValueArray();
    }

    /// <summary>
    /// Scans a register method's type parameters for typed-identification markers (D-08). Walks ALL type
    /// parameters once — never returns early, which IS the fail-silent truncation fix this closes — and
    /// buckets each attribute hit by kind: a bare <c>[TypedIdentification]</c> hit contributes to the
    /// single same-registry marker name (D-04, first-wins, unchanged <see cref="ComputeIdentificationType"/>
    /// consumer contract); a <c>[TypedIdentification&lt;TTarget&gt;]</c> hit — detected via
    /// <see cref="ITypeSymbol.IsGenericType"/> on the matched attribute class, since
    /// <see cref="DisplayFormats.NamespaceAndType"/>'s <c>GenericsOptions.None</c> already collapses both
    /// the bare and generic forms to the same base name (mirrors the <c>UseResourceFileAttribute</c>
    /// open-generic idiom at <c>RegistryShapeAnalyzer.cs:134-139</c>) — appends
    /// (typeParameter.Name, targetRegistryFqn, targetCategoryKey) to the cross-registry list (D-05). The
    /// target category key is resolved directly off the target's live symbol via
    /// <see cref="TryExtractRegistryKey"/> — this ALWAYS runs on a live <see cref="ITypeSymbol"/> (the
    /// attribute's own type argument), regardless of whether the target registry is declared in this
    /// compilation or referenced, so no cross-assembly metadata round-trip is needed for the target side
    /// (D-03). Empty when the target isn't a recognizable <c>[Registry]</c> type — Plan 04's alias
    /// emission surfaces this as a loud <c>extension()</c> compile error, never a silently-skipped alias.
    /// </summary>
    internal static TypedIdentificationExtraction ExtractTypedIdentificationMarkers(IMethodSymbol method)
    {
        string? bareMarker = null;
        var crossMarkers = new ImmutableValueArray<(string ParamName, string TargetRegistryFqn, string TargetCategoryKey)>.Builder();

        foreach (var typeParameter in method.TypeParameters)
        {
            foreach (var attribute in typeParameter.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) != TypedIdentificationAttribute)
                    continue;

                if (attributeClass.IsGenericType && attributeClass.TypeArguments.Length == 1)
                {
                    var targetType = attributeClass.TypeArguments[0];
                    var targetRegistryFqn = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var targetCategoryKey = targetType is INamedTypeSymbol targetNamedType
                        && TryExtractRegistryKey(targetNamedType, out var key)
                        ? key
                        : string.Empty;
                    crossMarkers.Add((typeParameter.Name, targetRegistryFqn, targetCategoryKey));
                }
                else
                {
                    // D-04: at most one bare marker feeds ComputeIdentificationType; first-wins mirrors the
                    // prior scalar semantics. Registry-wide ambiguity across methods is Plan 05's analyzer.
                    bareMarker ??= typeParameter.Name;
                }
            }
        }

        return new TypedIdentificationExtraction(bareMarker, crossMarkers.ToImmutableValueArray());
    }

    /// <summary>
    /// Captures the register-method-side resolution inputs Plan 04's pure-string constraint-guided walk
    /// consumes: the method's type parameters in declaration order, and — for every type parameter whose
    /// constraints reference another type parameter — a <see cref="RegisterConstraintRef"/> resolution-map
    /// entry. Loops ALL type parameters (not just the anchor), so 2+-type-parameter methods of either
    /// source kind capture their full generic structure.
    /// </summary>
    internal static void CaptureTypeParameterResolutionInputs(
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        out ImmutableValueArray<string> typeParameterNames,
        out ImmutableValueArray<RegisterConstraintRef> constraintRefs)
    {
        var namesBuilder = new ImmutableValueArray<string>.Builder();
        var refsBuilder = new ImmutableValueArray<RegisterConstraintRef>.Builder();

        foreach (var typeParameter in typeParameters)
        {
            namesBuilder.Add(typeParameter.Name);

            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                if (TryBuildConstraintRef(typeParameter.Name, constraintType, typeParameters,
                        out var constraintRef))
                {
                    refsBuilder.Add(constraintRef);
                }
            }
        }

        typeParameterNames = namesBuilder.ToImmutableValueArray();
        constraintRefs = refsBuilder.ToImmutableValueArray();
    }

    /// <summary>
    /// Builds the value parameter's constructed-generic structure for value-source resolution (D-03), e.g.
    /// <c>Wrapper&lt;T1, T2&gt;</c> in <c>RegisterX&lt;T1, T2&gt;(Identification, Wrapper&lt;T1, T2&gt;)</c>.
    /// Null for a bare-<c>T</c> or non-generic value parameter — mirrors the symmetry the operator required
    /// between the value-source and type-source capture paths.
    /// </summary>
    internal static RegisterConstraintRef? BuildValueParameterGeneric(ITypeSymbol parameterType,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (parameterType is not INamedTypeSymbol { IsGenericType: true }) return null;

        return TryBuildConstraintRef(string.Empty, parameterType, typeParameters, out var constraintRef)
            ? constraintRef
            : null;
    }

    /// <summary>
    /// Shared builder for both a type parameter's constructed-generic constraint
    /// (<see cref="CaptureTypeParameterResolutionInputs"/>) and a value parameter's constructed-generic
    /// structure (<see cref="BuildValueParameterGeneric"/>). Only produces a ref when at least one argument
    /// position references another of the method's type parameters — a constraint/wrapper over purely
    /// concrete types carries no resolution information for Plan 04's walk.
    /// </summary>
    private static bool TryBuildConstraintRef(string owningTypeParameterName, ITypeSymbol constructedType,
        ImmutableArray<ITypeParameterSymbol> typeParameters, out RegisterConstraintRef constraintRef)
    {
        constraintRef = null!;
        if (constructedType is not INamedTypeSymbol { IsGenericType: true } named) return false;

        var argNamesBuilder = new ImmutableValueArray<string>.Builder();
        var mentionsAny = false;
        foreach (var typeArg in named.TypeArguments)
        {
            var referenced = typeArg is ITypeParameterSymbol argTp
                ? typeParameters.FirstOrDefault(tp => SymbolEqualityComparer.Default.Equals(tp, argTp))
                : null;
            argNamesBuilder.Add(referenced?.Name ?? string.Empty);
            if (referenced is not null) mentionsAny = true;
        }

        if (!mentionsAny) return false;

        var openFqn = named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        constraintRef = new RegisterConstraintRef(owningTypeParameterName, openFqn,
            argNamesBuilder.ToImmutableValueArray());
        return true;
    }

    /// <summary>
    /// True when <paramref name="parameterType"/> either IS the register method's type parameter
    /// (bare-T value shape, e.g. <c>RegisterComponent&lt;T&gt;(Identification, T)</c>) or is a
    /// constructed generic that mentions it among its type arguments (wrapper-over-T shape, e.g.
    /// <c>RegisterSetting&lt;T&gt;(Identification, SettingDefinition&lt;T&gt;)</c>). The wrapper case
    /// carries the closed generic value type through registration — C# infers T from the provider
    /// return type at the emitted, type-argument-free call site.
    /// </summary>
    internal static bool MentionsTypeParameter(ITypeSymbol parameterType, ITypeParameterSymbol typeParameter)
    {
        if (SymbolEqualityComparer.Default.Equals(parameterType, typeParameter)) return true;
        return parameterType is INamedTypeSymbol { IsGenericType: true } named &&
               named.TypeArguments.Any(arg => SymbolEqualityComparer.Default.Equals(arg, typeParameter));
    }

    private static string? ExtractKeyedFactoryMarkerTBase(IMethodSymbol m)
    {
        foreach (var attr in m.GetAttributes())
        {
            if (attr.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType)
                != KeyedFactoryGenerationMarkerOpenName) continue;
            if (attr.AttributeClass.TypeArguments.FirstOrDefault() is INamedTypeSymbol tBase)
                return tBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        return null;
    }

    internal static void ParseTypeParameterConstraints(ITypeParameterSymbol parameter,
        out TypeConstraintFlag constraintFlag, out ImmutableValueArray<string> typeConstraints)
    {
        constraintFlag = TypeConstraintFlag.None;
        var constraintBuilder = new ImmutableValueArray<string>.Builder();

        if (parameter.HasReferenceTypeConstraint) constraintFlag |= TypeConstraintFlag.ReferenceType;
        if (parameter.HasValueTypeConstraint) constraintFlag |= TypeConstraintFlag.ValueType;
        if (parameter.AllowsRefLikeType) constraintFlag |= TypeConstraintFlag.AllowRefLike;
        if (parameter.HasUnmanagedTypeConstraint) constraintFlag |= TypeConstraintFlag.Unmanaged;
        if (parameter.HasNotNullConstraint) constraintFlag |= TypeConstraintFlag.NotNull;
        if (parameter.HasConstructorConstraint) constraintFlag |= TypeConstraintFlag.ParameterlessConstructor;

        foreach (var constraintType in parameter.ConstraintTypes)
        {
            constraintBuilder.Add(constraintType.ToDisplayString(DisplayFormats.NamespaceAndType));
        }

        typeConstraints = constraintBuilder.ToImmutableValueArray();
    }

    internal static ImmutableValueArray<RegistryModel> ExtractModels(Compilation compilation)
    {
        // Not covered by tests due to complexity; be careful with changes.

        var models = new ImmutableValueArray<RegistryModel>.Builder();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) continue;

            foreach (var attributeData in assembly.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) is not RegistryMetadataAttribute) continue;
                if (attributeData.AttributeClass.TypeArguments.Length != 1) continue;

                if (TryExtractRegistryFromAssemblyAttribute(attributeData.AttributeClass.TypeArguments.First(),
                        out var model) && model is not null)
                    models.Add(model);
            }
        }

        return models.ToImmutableValueArray();
    }


    internal static bool TryExtractRegistryFromAssemblyAttribute(ITypeSymbol metadata,
        out RegistryModel? model)
    {
        var reader = new SymbolMetadataReader(metadata);

        var methods = Of("RegisterMethods").Split([';'], StringSplitOptions.RemoveEmptyEntries);

        var methodModels = new ImmutableValueArray<RegisterMethodModel>.Builder();

        var allValid = true;
        foreach (var methodName in methods)
        {
            // Method metadata is now an inner class with the same name as the method
            if (TryParseRegisterMethod((INamedTypeSymbol)metadata, methodName, out var methodModel))
            {
                methodModels.Add(methodModel!);
                continue;
            }

            allValid = false;
        }

        // Parse ResourceFiles
        var resourceFiles = new ImmutableValueArray<(string Key, bool Required, bool Primary)>.Builder();
        var resourceFilesStr = Of("ResourceFiles");
        if (!string.IsNullOrEmpty(resourceFilesStr))
        {
            var files = resourceFilesStr.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in files)
            {
                var parts = file.Split(':');
                if (parts.Length == 3 &&
                    int.TryParse(parts[1], out var requiredInt) &&
                    int.TryParse(parts[2], out var primaryInt))
                {
                    resourceFiles.Add((parts[0], requiredInt == 1, primaryInt == 1));
                }
            }
        }

        // The metadata class itself lives in the declaring assembly's SG output namespace —
        // capture it so cross-assembly consumers can reference the configurator attribute by FQN.
        var declaringSgNamespace = ((INamedTypeSymbol)metadata).ContainingNamespace?.ToDisplayString();

        // Optional owning-module linkage. Read directly off the symbol (bypassing reader.Of so AllValid is
        // never affected by an absent field) so pre-migration metadata still parses.
        string? owningModule = null;
        var owningModuleField = ((INamedTypeSymbol)metadata).GetMembers("OwningModule")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (owningModuleField is { IsConst: true, HasConstantValue: true, ConstantValue: string owningModuleData }
            && !string.IsNullOrEmpty(owningModuleData))
        {
            owningModule = owningModuleData;
        }

        // D-06: optional registry-level alias suffix, same bypass-reader.Of() idiom as OwningModule so
        // pre-Plan-04 metadata (predating this field) still parses cleanly.
        string? aliasSuffix = null;
        var aliasSuffixField = ((INamedTypeSymbol)metadata).GetMembers("AliasSuffix")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (aliasSuffixField is { IsConst: true, HasConstantValue: true, ConstantValue: string aliasSuffixData }
            && !string.IsNullOrEmpty(aliasSuffixData))
        {
            aliasSuffix = aliasSuffixData;
        }

        model = new RegistryModel(
            Of("TypeName"),
            Of("Key"),
            Of("ContainingNamespace"),
            reader.OfBool("IsExternal"),
            methodModels.ToImmutableValueArray(),
            resourceFiles.ToImmutableValueArray(),
            declaringSgNamespace,
            owningModule,
            aliasSuffix);

        allValid &= reader.AllValid;

        if (!allValid) model = null;

        return allValid;

        string Of(string fieldName) => reader.Of(fieldName);
    }

    internal static bool TryParseRegisterMethod(INamedTypeSymbol parentMetadata, string methodName,
        out RegisterMethodModel? model)
    {
        model = null;

        // Look for the inner class within the parent metadata type
        var methodMetadata = parentMetadata.GetTypeMembers(methodName).FirstOrDefault();
        if (methodMetadata is null)
        {
            return false;
        }

        var reader = new SymbolMetadataReader(methodMetadata);

        // Parse PrimaryParameterKind as int
        var parameterKind = (PrimaryParameterKind)reader.OfInt("PrimaryParameterKind");

        // Parse TypeConstraintFlag as int
        var constraint = (TypeConstraintFlag)reader.OfInt("Constraint");

        // Parse TypeConstraint as semicolon-separated string
        var typeConstraints = new ImmutableValueArray<string>.Builder();
        var typeConstraintStr = Of("TypeConstraint");
        // TypeConstraint should exist but may be empty
        if (!string.IsNullOrEmpty(typeConstraintStr))
        {
            var constraints = typeConstraintStr.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var constraintType in constraints)
            {
                typeConstraints.Add(constraintType);
            }
        }

        // W1 LOCKED: read optional KeyedFactoryMarkerTBase directly off the symbol,
        // bypassing reader.Of() so AllValid is never affected by an absent optional field.
        // Metadata that omits this field continues to parse cleanly.
        string? markerTBase = null;
        var markerField = methodMetadata.GetMembers("KeyedFactoryMarkerTBase")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (markerField is { IsConst: true, HasConstantValue: true, ConstantValue: string markerData }
            && !string.IsNullOrEmpty(markerData))
        {
            markerTBase = markerData;
        }

        // Optional KeyedFactoryMarkerTKey — same roundtrip shape. Absent on pre-260511-lio metadata.
        string? markerTKey = null;
        var markerTKeyField = methodMetadata.GetMembers("KeyedFactoryMarkerTKey")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (markerTKeyField is { IsConst: true, HasConstantValue: true, ConstantValue: string markerTKeyData }
            && !string.IsNullOrEmpty(markerTKeyData))
        {
            markerTKey = markerTKeyData;
        }

        // FULL per-type-parameter roundtrip (D-03/D-08 cross-assembly fix) — all four read directly off
        // the symbol, bypassing reader.Of() so AllValid is never affected by absent/legacy metadata.

        string? typedIdentificationTypeParameterName = null;
        var typedIdentificationField = methodMetadata.GetMembers("TypedIdentificationTypeParameterName")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (typedIdentificationField is
            { IsConst: true, HasConstantValue: true, ConstantValue: string typedIdentificationData }
            && !string.IsNullOrEmpty(typedIdentificationData))
        {
            typedIdentificationTypeParameterName = typedIdentificationData;
        }

        var typeParameterNames = new ImmutableValueArray<string>.Builder();
        var typeParameterNamesField = methodMetadata.GetMembers("TypeParameterNames")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (typeParameterNamesField is
            { IsConst: true, HasConstantValue: true, ConstantValue: string typeParameterNamesData }
            && !string.IsNullOrEmpty(typeParameterNamesData))
        {
            foreach (var name in typeParameterNamesData.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                typeParameterNames.Add(name);
            }
        }

        var constraintRefs = new ImmutableValueArray<RegisterConstraintRef>.Builder();
        var constraintRefsField = methodMetadata.GetMembers("ConstraintRefs")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (constraintRefsField is
            { IsConst: true, HasConstantValue: true, ConstantValue: string constraintRefsData }
            && !string.IsNullOrEmpty(constraintRefsData))
        {
            foreach (var encodedRef in constraintRefsData.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryDecodeConstraintRef(encodedRef, out var decodedRef))
                {
                    constraintRefs.Add(decodedRef);
                }
            }
        }

        RegisterConstraintRef? valueParameterGeneric = null;
        var valueParameterGenericField = methodMetadata.GetMembers("ValueParameterGeneric")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (valueParameterGenericField is
            { IsConst: true, HasConstantValue: true, ConstantValue: string valueParameterGenericData }
            && !string.IsNullOrEmpty(valueParameterGenericData)
            && TryDecodeConstraintRef(valueParameterGenericData, out var decodedValueParameterGeneric))
        {
            valueParameterGeneric = decodedValueParameterGeneric;
        }

        // D-07: external registries provide cross-registry portion info manually through this SAME
        // decode shape — direct-symbol const-field read, bypassing reader.Of() so legacy metadata
        // predating this field never trips reader.AllValid.
        var crossRegistryMarkers = new ImmutableValueArray<(string ParamName, string TargetRegistryFqn, string TargetCategoryKey)>.Builder();
        var crossRegistryMarkersField = methodMetadata.GetMembers("CrossRegistryMarkers")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (crossRegistryMarkersField is
            { IsConst: true, HasConstantValue: true, ConstantValue: string crossRegistryMarkersData }
            && !string.IsNullOrEmpty(crossRegistryMarkersData))
        {
            foreach (var encodedMarker in crossRegistryMarkersData.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = encodedMarker.Split('|');
                if (parts.Length == 3)
                {
                    crossRegistryMarkers.Add((parts[0], parts[1], parts[2]));
                }
            }
        }

        model = new RegisterMethodModel(
            Of("FunctionName"),
            parameterKind,
            constraint,
            typeConstraints.ToImmutableValueArray(),
            markerTBase,
            markerTKey,
            typedIdentificationTypeParameterName,
            TypeParameterNames: typeParameterNames.ToImmutableValueArray(),
            ConstraintRefs: constraintRefs.ToImmutableValueArray(),
            ValueParameterGeneric: valueParameterGeneric,
            CrossRegistryMarkers: crossRegistryMarkers.ToImmutableValueArray()
        );

        return reader.AllValid;

        string Of(string fieldName) => reader.Of(fieldName);
    }

    /// <summary>
    /// Decodes a single <see cref="RegisterConstraintRef"/> from its metadata-roundtrip encoding
    /// (<c>{TypeParameterName}|{ConstraintOpenDefinitionFqn}|{arg0,arg1,...}</c>). Splits on <c>|</c> first
    /// (never <c>,</c>) to always get exactly 3 parts, then splits the arg segment on <c>,</c> WITHOUT
    /// removing empty entries so positional empties (concrete, non-type-parameter argument slots) survive.
    /// </summary>
    private static bool TryDecodeConstraintRef(string encoded, out RegisterConstraintRef constraintRef)
    {
        constraintRef = null!;
        var parts = encoded.Split('|');
        if (parts.Length != 3) return false;

        var argNames = parts[2].Split([','], StringSplitOptions.None).ToImmutableValueArray();
        constraintRef = new RegisterConstraintRef(parts[0], parts[1], argNames);
        return true;
    }

    struct SymbolMetadataReader(ITypeSymbol symbol)
    {
        public bool AllValid { get; private set; } = true;

        public string Of(string fieldName)
        {
            var field = symbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
            if (field is not { IsConst: true, HasConstantValue: true, ConstantValue: string data })
            {
                AllValid = false;
                return null!;
            }

            return data;
        }

        public int OfInt(string fieldName)
        {
            var field = symbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
            if (field is not { IsConst: true, HasConstantValue: true } || field.ConstantValue is not int data)
            {
                AllValid = false;
                return 0;
            }

            return data;
        }

        public bool OfBool(string fieldName)
        {
            var field = symbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
            if (field is not { IsConst: true, HasConstantValue: true } || field.ConstantValue is not bool data)
            {
                AllValid = false;
                return false;
            }

            return data;
        }
    }

    internal static bool RenderRegistryAttributes(RegistryModel model, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}_Attributes.g.cs";

        // Always use keyed properties: {PascalCase(Key)}File
        var files = model.ResourceFiles
            .OrderBy(r => r.Key)
            .Select(r => new { Prop = StringCase.ToPascalCase(r.Key) + "File", IsNullable = !r.Required })
            .ToArray();

        var templateModel = new
        {
            Namespace = model.ContainingNamespace,
            RegistryName = model.TypeName,
            Key = model.Key,
            Methods = model.RegisterMethods.Where(m => m.PrimaryParameterKind != PrimaryParameterKind.None).Select(m => new { Name = m.FunctionName, Files = files }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryAttributes.liquid", templateModel, out code);
    }

    // TODO: Analyzer: prevent duplicate registry class names across namespaces (assumption: unique type names).

    /// <summary>
    /// Extracts the registry key (Identifier) from a registry type symbol.
    /// Used by external SGs to look up registry category information.
    /// </summary>
    public static bool TryExtractRegistryKey(INamedTypeSymbol registryType, out string key)
    {
        key = string.Empty;

        var registryAttribute = registryType.GetAttributes().FirstOrDefault(x =>
        {
            var attrClass = x.AttributeClass;
            // Check if attribute inherits from RegistryAttribute
            while (attrClass is not null)
            {
                if (attrClass.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryMarkerAttribute)
                    return true;
                attrClass = attrClass.BaseType;
            }
            return false;
        });

        if (registryAttribute is null) return false;

        var identifierEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key is RegistryAttributeIdField);
        if (identifierEntry.Value.Value is not string id || string.IsNullOrWhiteSpace(id))
            return false;

        key = id;
        return true;
    }
}
