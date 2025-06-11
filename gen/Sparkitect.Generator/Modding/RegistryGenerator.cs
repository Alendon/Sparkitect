using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YamlDotNet.Serialization;

namespace Sparkitect.Generator.Modding;

[Generator]
public partial class RegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        var assemblyRegistryModelsProvider = context.CompilationProvider.Select((compilation, _) =>
        {
            return ExtractModels(compilation);
        });

        context.RegisterSourceOutput(symbolRegistryModelsProvider.Combine(buildSettings), OutputRegistryMetadata);
        context.RegisterSourceOutput(symbolRegistryModelsProvider.Collect().Combine(buildSettings),
            OutputRegistryConfigurator);

        var registryMapProvider = symbolRegistryModelsProvider.Collect().Combine(assemblyRegistryModelsProvider)
            .Select((x, _) => RegistryMap.Create(x));

        /*
         * When looking at registrations with registry methods of the current compilation
         * The Attributes are not available at SG time (SG produces them)
         * This leads to the problem, that for compilation introduced RegistryMethods they might duplicate with other attributes
         * If this happens, aka the Generator reports that it is an Error Type and more than one type with this name exists,
         * Generating a partial class as a "dummy" fixes this issue, by introducing the target attribute class
         */


        var resourceFileRegistrationProvider = context.AdditionalTextsProvider
            .Where(x => x.Path.EndsWith(ResourceFileSuffix))
            .Select(ParseResourceYaml);
    }

    private static ValueCompareSet<FileRegistrationEntry> ParseResourceYaml(AdditionalText text,
        CancellationToken cancellation)
    {
        ValueCompareSet<FileRegistrationEntry> result = [];

        var sourceText = text.GetText(cancellation);
        if (sourceText is null) return result;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlContent = deserializer.Deserialize<ResourceYamlRoot>(sourceText.ToString());

            if (yamlContent?.Registries is null)
                return result;

            foreach (var registry in yamlContent.Registries)
            {
                if (string.IsNullOrWhiteSpace(registry.Key) || registry.Value is null) continue;

                foreach (var entry in registry.Value)
                {
                    if (string.IsNullOrWhiteSpace(entry.Id)) continue;

                    ValueCompareSet<(string fileId, string fileName)> files = [];
                    if (entry.Files is not null)
                    {
                        foreach (var file in entry.Files)
                        {
                            files.Add((file.Key, file.Value));
                        }
                    }

                    result.Add(new FileRegistrationEntry(
                        registry.Key,
                        entry.Id!,
                        entry.SymbolName,
                        files
                    ));
                }
            }
        }
        catch
        {
            // Return empty list if parsing fails
        }

        return result;
    }


    internal static RegistryModel? ExtractModel(INamedTypeSymbol symbol, AttributeData registryAttribute)
    {
        var identifierEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key is RegistryAttributeIdField);
        if (identifierEntry.Value.Value is not string id || string.IsNullOrWhiteSpace(id)) return null;

        var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(namespaceName) ||
            symbol.ContainingNamespace?.IsGlobalNamespace is true) return null;


        //TODO Registry Analyzer: Registry class cannot live outside namespace
        //General Analyzer (Utility class): No Type outside defined root namespace
        //Alternative: Define "GeneratorBaseNamespace", where the generator places it general entries
        return new RegistryModel(symbol.Name, id, namespaceName!, ExtractRegisterMethods(symbol),
            ExtractResourceFiles(symbol));
    }

    internal static ValueCompareSet<(string Identifier, bool Required)> ExtractResourceFiles(INamedTypeSymbol symbol)
    {
        ValueCompareSet<(string Identifier, bool Required)> result = [];

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

        return result;
    }

    internal static ValueCompareSet<RegisterMethodModel> ExtractRegisterMethods(INamedTypeSymbol symbol)
    {
        ValueCompareSet<RegisterMethodModel> result = new();

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
                    [parameter.ToDisplayString(DisplayFormats.NamespaceAndType)]));

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

        return result;
    }

    internal static void ParseTypeParameterConstraints(ITypeParameterSymbol parameter,
        out TypeConstraintFlag constraintFlag, out ValueCompareSet<string> typeConstraints)
    {
        constraintFlag = TypeConstraintFlag.None;
        typeConstraints = [];

        if (parameter.HasReferenceTypeConstraint) constraintFlag &= TypeConstraintFlag.ReferenceType;
        if (parameter.HasValueTypeConstraint) constraintFlag &= TypeConstraintFlag.ValueType;
        if (parameter.AllowsRefLikeType) constraintFlag &= TypeConstraintFlag.AllowRefLike;
        if (parameter.HasUnmanagedTypeConstraint) constraintFlag &= TypeConstraintFlag.Unmanaged;
        if (parameter.HasNotNullConstraint) constraintFlag &= TypeConstraintFlag.NotNull;
        if (parameter.HasConstructorConstraint) constraintFlag &= TypeConstraintFlag.ParameterlessConstructor;

        foreach (var constraintType in parameter.ConstraintTypes)
        {
            typeConstraints.Add(constraintType.ToDisplayString(DisplayFormats.NamespaceAndType));
        }
    }

    internal static ValueCompareSet<RegistryModel> ExtractModels(Compilation compilation)
    {
        //WARNING This function is currently not tested because of the complexity.
        //Be careful with changes
        //TODO Validate manually with the MinimalTestMod that this is functional

        ValueCompareSet<RegistryModel> models = new();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) continue;

            foreach (var attributeData in assembly.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToDisplayString() is not RegistryMetadataAttribute) continue;
                if (attributeData.AttributeClass.TypeArguments.Length != 1) continue;

                if (TryExtractRegistryFromAssemblyAttribute(attributeData.AttributeClass.TypeArguments.First(),
                        compilation,
                        out var model) && model is not null)
                    models.Add(model);
            }
        }

        return models;
    }


    internal static bool TryExtractRegistryFromAssemblyAttribute(ITypeSymbol metadata, Compilation compilation,
        out RegistryModel? model)
    {
        var reader = new SymbolMetadataReader(metadata);

        var methods = Of("RegisterMethods").Split(';');

        ValueCompareSet<RegisterMethodModel> methodModels = new();

        var allValid = true;
        foreach (var methodMetadata in methods)
        {
            if (TryParseRegisterMethod(methodMetadata, compilation, out var methodModel))
            {
                methodModels.Add(methodModel!);
                continue;
            }

            allValid = false;
        }

        model = new RegistryModel(
            Of("TypeName"),
            Of("Key"),
            Of("ContainingNamespace"), methodModels);

        allValid &= reader.AllValid;

        if (!allValid) model = null;

        return allValid;

        string Of(string fieldName) => reader.Of(fieldName);
    }

    internal static bool TryParseRegisterMethod(string methodName, Compilation compilation,
        out RegisterMethodModel? model)
    {
        model = null;

        var methodMetadata = compilation.GetTypeByMetadataName(methodName);
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
        ValueCompareSet<string> typeConstraints = [];
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
            typeConstraints
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
            if (field is not { IsConst: true, HasConstantValue: true } || field.ConstantValue is not string data ||
                string.IsNullOrWhiteSpace(data))
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

    private class ResourceYamlRoot
    {
        public Dictionary<string, List<ResourceYamlEntry>>? Registries { get; set; }
    }

    private class ResourceYamlEntry
    {
        public string? Id { get; set; }
        public string? SymbolName { get; set; }
        public Dictionary<string, string>? Files { get; set; }
    }
}