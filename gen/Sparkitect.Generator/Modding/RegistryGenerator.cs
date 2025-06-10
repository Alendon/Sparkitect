using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparkitect.Generator.Modding;

[Generator]
public partial class RegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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
        
        //context.RegisterSourceOutput(symbolRegistryModelsProvider, OutputRegistryMetadata);
        //context.RegisterSourceOutput(symbolRegistryModelsProvider.Collect(), OutputRegistryConfigurator);
    }

    internal static RegistryModel? ExtractModel(INamedTypeSymbol symbol, AttributeData registryAttribute)
    {
        var identifierEntry = registryAttribute.NamedArguments.FirstOrDefault(x => x.Key is RegistryAttributeIdField);
        if (identifierEntry.Value.Value is not string id || string.IsNullOrWhiteSpace(id)) return null;

        var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(namespaceName) || symbol.ContainingNamespace?.IsGlobalNamespace is true) return null;

//TODO Registry Analyzer: Registry class cannot live outside namespace
//General Analyzer (Utility class): No Type outside defined root namespace
//Alternative: Define "GeneratorBaseNamespace", where the generator places it general entries
        return new RegistryModel(symbol.Name, id, namespaceName!);
    }

    internal static ValueCompareList<RegistryModel> ExtractModels(Compilation compilation)
    {
        //WARNING This function is currently not tested because of the complexity.
        //Be careful with changes
        //TODO Validate manually with the MinimalTestMod that this is functional
        
        ValueCompareList<RegistryModel> models = new();
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

        return models;
    }


    internal static bool TryExtractRegistryFromAssemblyAttribute(ITypeSymbol metadata, out RegistryModel? model)
    {
        var allValid = true;

        model = new RegistryModel(
            Of("TypeName"),
            Of("Key"),
            Of("ContainingNamespace"));

        if (!allValid) model = null;

        return allValid;

        string Of(string fieldName)
        {
            var field = metadata.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
            if (field is not { IsConst: true, HasConstantValue: true } || field.ConstantValue is not string data || string.IsNullOrWhiteSpace(data))
            {
                allValid = false;
                return null!;
            }

            return data;
        }
    }
}