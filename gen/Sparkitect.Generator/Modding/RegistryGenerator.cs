using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sparkitect.Generator.Modding;

[Generator]
public partial class RegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        var buildSettings = context.GetModBuildSettings();

        var symbolRegistryModelsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(RegistryMarkerAttribute,
            (node, _) => node is ClassDeclarationSyntax, (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol symbol) return null;
                if (!symbol.AllInterfaces.Any(i =>
                        i.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryInterface)) return null;

                var registryAttribute = symbol.GetAttributes().FirstOrDefault(x =>
                    x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) is RegistryMarkerAttribute);
                if (registryAttribute is null) return null;

                return ExtractModel(symbol, registryAttribute);
            }).NotNull();

        var assemblyRegistryModelsProvider =
            context.CompilationProvider.Select((compilation, _) => ExtractModels(compilation));

        // Emit per-registry nested attributes early to stabilize attribute resolution in subsequent passes
        context.RegisterSourceOutput(symbolRegistryModelsProvider, OutputRegistryAttributes);

        context.RegisterSourceOutput(symbolRegistryModelsProvider.Combine(buildSettings), OutputRegistryMetadata);
        context.RegisterSourceOutput(symbolRegistryModelsProvider.Collect().Combine(buildSettings),
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


        // Resource files: keep parse as a separate result carrying path + entries
        var resourceFileRegistrationProvider = context.AdditionalTextsProvider
            .Where(x =>
                x.Path.EndsWith(ResourceFileSuffix))
            .Select((text, cancellation) => (
                text.Path,
                Entries:
                ParseResourceYaml(text, cancellation)));

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

        // Outputs per unit: registrations + ID properties
        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputRegistrationsUnit(spc, (pair.Left, pair.Right)));
        context.RegisterSourceOutput(resourceUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputRegistrationsUnit(spc, (pair.Left, pair.Right)));

        context.RegisterSourceOutput(providerUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputIdPropertiesUnit(spc, (pair.Left, pair.Right)));
        context.RegisterSourceOutput(resourceUnitsProvider.Combine(buildSettings),
            static (spc, pair) => OutputIdPropertiesUnit(spc, (pair.Left, pair.Right)));
    }


    internal static ImmutableValueArray<FileRegistrationEntry> ParseResourceYaml(AdditionalText text,
        CancellationToken cancellation)
    {
        var result = new ImmutableValueArray<FileRegistrationEntry>.Builder();

        var sourceText = text.GetText(cancellation);
        if (sourceText is null) return result.ToImmutableValueArray();


        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var raw = sourceText.ToString();

        var root = deserializer.Deserialize<Dictionary<string, List<ResourceYamlEntry>>>(raw);
        if (root is not null)
        {
            foreach (var registry in root)
            {
                if (string.IsNullOrWhiteSpace(registry.Key) || registry.Value is null) continue;

                foreach (var entry in registry.Value)
                {
                    if (string.IsNullOrWhiteSpace(entry.Id)) continue;
                    if (!IsSnakeCase(entry.Id!)) continue;

                    var files = new ImmutableValueArray<(string fileId, string fileName)>.Builder();
                    if (entry.Files is not null)
                    {
                        foreach (var file in entry.Files.OrderBy(kvp => kvp.Key))
                        {
                            files.Add((file.Key, file.Value));
                        }
                    }

                    // Single-file support: map to implicit key "default"
                    if (!string.IsNullOrWhiteSpace(entry.File))
                    {
                        files.Add(("default", entry.File));
                    }

                    var methodSplitIndex = registry.Key.LastIndexOf('.');

                    result.Add(new FileRegistrationEntry(
                        registry.Key.Substring(0, methodSplitIndex),
                        registry.Key.Substring(methodSplitIndex+1),
                        entry.Id!,
                        files.ToImmutableValueArray()
                    ));
                }
            }
        }


        return result.ToImmutableValueArray();
    }

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


            var files = e.Files.ToImmutableValueArray();
            var entry = new RegistrationEntry(e.Id, EntryKind.Resource, e.MethodName, string.Empty, string.Empty, files,
                []);
            bucket.builder.Add(entry);
            map[key] = bucket;
        }

        // Freeze per registry into units
        var units = new ImmutableValueArray<RegistrationUnit>.Builder();
        foreach (var kvp in map)
        {
            // Sort entries by Id, and files by fileId for determinism
            var sortedEntries = kvp.Value.builder
                .OrderBy(x => x.Id)
                .Select(x => new RegistrationEntry(
                    x.Id,
                    x.Kind,
                    x.MethodName,
                    x.ProviderContainingType,
                    x.ProviderMemberName,
                    x.Files.ToImmutableValueArray(),
                    x.DiParameters
                ))
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
                // Ensure files within entry are ordered by fileId for determinism
                var orderedFiles = e.Files.OrderBy(f => f.fileId).ToImmutableValueArray();
                builder.Add(new RegistrationEntry(
                    e.Id,
                    e.Kind,
                    e.MethodName,
                    e.ProviderContainingType,
                    e.ProviderMemberName,
                    orderedFiles,
                    e.DiParameters));
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
        if (!IsSnakeCase(id)) return null;

        var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(namespaceName) ||
            symbol.ContainingNamespace?.IsGlobalNamespace is true) return null;


        //TODO Registry Analyzer: Registry class cannot live outside namespace
        //General Analyzer (Utility class): No Type outside defined root namespace
        //Alternative: Define "GeneratorBaseNamespace", where the generator places it general entries
        return new RegistryModel(symbol.Name, id, namespaceName!, ExtractRegisterMethods(symbol),
            ExtractResourceFiles(symbol));
    }

    internal static ImmutableValueArray<(string Identifier, bool Required)> ExtractResourceFiles(
        INamedTypeSymbol symbol)
    {
        var result = new ImmutableValueArray<(string Identifier, bool Required)>.Builder();

        var attributes = symbol.GetAttributes().Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) is UseResourceFileAttribute);

        foreach (var attributeData in attributes)
        {
            var identifier = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Identifier")
                .Value.Value as string;

            var required = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Required")
                .Value.Value;

            if (identifier is null) continue;

            result.Add((identifier, required is true));
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
            //Methods with more than one parameter of either kind are not allowed
            if (method.TypeParameters.Length > 1 || method.Parameters.Length == 0 ||
                method.Parameters.Length > 2) continue;

            if (method.Parameters.First().Type.ToDisplayString(DisplayFormats.NamespaceAndType) !=
                IdentificationStruct) continue;

            //Method registration
            if (method.Parameters.Length == 2)
            {
                var parameter = method.Parameters.ElementAt(1);

                if (method.TypeParameters.Length == 1)
                {
                    //If it is a generic method, the argument must be the generic type
                    if (!SymbolEqualityComparer.Default.Equals(parameter.Type, method.TypeParameters.First())) continue;

                    ParseTypeParameterConstraints(method.TypeParameters.First(), out var constraintFlag,
                        out var typeConstraints);
                    result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.GenericValue, constraintFlag,
                        typeConstraints));

                    continue;
                }

                result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.Value, TypeConstraintFlag.None,
                    ((string[])[parameter.ToDisplayString(DisplayFormats.NamespaceAndType)]).ToImmutableValueArray()));

                continue;
            }

            //Type registration
            if (method.TypeParameters.Length == 1)
            {
                ParseTypeParameterConstraints(method.TypeParameters.First(), out var constraintFlag,
                    out var typeConstraints);
                result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.Type, constraintFlag,
                    typeConstraints));

                continue;
            }


            //Resource file registration
            result.Add(new RegisterMethodModel(method.Name, PrimaryParameterKind.None, TypeConstraintFlag.None, []));
        }

        return result.ToImmutableValueArray();
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
        //WARNING This function is currently not tested because of the complexity.
        //Be careful with changes
        //TODO Validate manually with the MinimalTestMod that this is functional

        var models = new ImmutableValueArray<RegistryModel>.Builder();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) continue;

            foreach (var attributeData in assembly.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToDisplayString() is not RegistryMetadataAttribute) continue;
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
        var resourceFiles = new ImmutableValueArray<(string identifier, bool optional)>.Builder();
        var resourceFilesStr = Of("ResourceFiles");
        if (!string.IsNullOrEmpty(resourceFilesStr))
        {
            var files = resourceFilesStr.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in files)
            {
                var parts = file.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var optionalInt))
                {
                    resourceFiles.Add((parts[0], optionalInt == 1));
                }
            }
        }

        model = new RegistryModel(
            Of("TypeName"),
            Of("Key"),
            Of("ContainingNamespace"),
            methodModels.ToImmutableValueArray(),
            resourceFiles.ToImmutableValueArray());

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

        model = new RegisterMethodModel(
            Of("FunctionName"),
            parameterKind,
            constraint,
            typeConstraints.ToImmutableValueArray()
        );

        return reader.AllValid;

        string Of(string fieldName) => reader.Of(fieldName);
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
    }


    internal static string ToSnakeCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }

        return sb.ToString();
    }

    internal static string ToPascalCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var parts = s.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
        {
            if (p.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p.Substring(1));
        }

        return sb.ToString();
    }

    internal static bool IsSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (ch == '_') continue;
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) continue;
            return false;
        }
        return true;
    }

    internal static bool RenderRegistryAttributes(RegistryModel model, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}_Attributes.g.cs";

        var isSingleFile = model.ResourceFiles.Count == 1;

        var files = isSingleFile
            ? new[]
            {
                new { Prop = "File", IsNullable = model.ResourceFiles.First().optional }
            }
            : model.ResourceFiles
                .OrderBy(r => r.identifier)
                .Select(r => new { Prop = ToPascalCase(r.identifier), IsNullable = r.optional })
                .ToArray();

        var templateModel = new
        {
            Namespace = model.ContainingNamespace,
            RegistryName = model.TypeName,
            Methods = model.RegisterMethods.Select(m => new { Name = m.FunctionName, Files = files }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryAttributes.liquid", templateModel, out code);
    }

    private class ResourceYamlEntry
    {
        public string? Id { get; set; }

        // Only Files or File can be set not both
        public Dictionary<string, string>? Files { get; set; }
        public string File { get; set; }
    }

    // TODO: Analyzer: prevent duplicate registry class names across namespaces (assumption: unique type names).
}
