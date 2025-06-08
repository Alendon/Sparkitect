using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.DI.DiUtils;

namespace Sparkitect.Generator.DI;

[Generator]
public class DiFactoryGenerator : IIncrementalGenerator
{
    /*
     * TODO: Key type validation for factory attributes
     * - Create analyzer to validate that all NamedArguments of Key/KeyProperty attributes are always strings
     * - Factory attribute validation itself is not part of DiFactoryAnalyzer but should be separate
     * 
     * TODO: Update source generation to enforce string-only direct keys
     * - Currently allows Identification and OneOf types but should enforce string only for direct keys
     * - Update ExtractKeyInfo method to validate key types during generation
     */

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all Singleton marked classes
        IncrementalValuesProvider<object> singletonModelProvider = context.SyntaxProvider.CreateSyntaxProvider<object?>(
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol
                    classSymbol) return null;

                var attributes = classSymbol.GetAttributes();

                var factoryAttribute = attributes.FirstOrDefault(x => FindFactoryMarker(x) is not null);
                if (factoryAttribute is null) return null;

                var factoryType = GetFactoryGenerationType(factoryAttribute.AttributeClass);

                return factoryType switch
                {
                    FactoryType.Service => ExtractServiceFactoryModelData(classSymbol),
                    FactoryType.Factory => ExtractKeyedFactoryModelData(classSymbol),
                    FactoryType.Entrypoint => ExtractEntrypointFactoryModelData(classSymbol),
                    _ => null
                };
            }).NotNull();

        context.RegisterSourceOutput(singletonModelProvider, (context, model) =>
        {
            string? code;
            string? fileName;

            switch (model)
            {
                case ServiceFactoryModel singletonModel:
                    if (RenderServiceFactory(singletonModel, out code, out fileName))
                        context.AddSource(fileName, code);
                    break;

                case EntrypointFactoryModel entrypointModel:
                    if (RenderEntrypointFactory(entrypointModel, out code, out fileName))
                        context.AddSource(fileName, code);
                    break;

                case KeyedFactoryModel keyedModel:
                    if (RenderKeyedFactory(keyedModel, out code, out fileName))
                        context.AddSource(fileName, code);
                    break;
            }
        });
    }


    internal static bool RenderServiceFactory(ServiceFactoryModel model, out string code, out string fileName)
    {
        fileName = $"{model.ImplementationTypeName}_Factory.g.cs";

        return FluidHelper.TryRenderTemplate("DI.SingletonFactory.liquid", model, out code);
    }

    internal static ServiceFactoryModel? ExtractServiceFactoryModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryMarker(x) is not null);
        if (factoryAttribute is null) return null;

        var factoryMarker = FindFactoryMarker(factoryAttribute);
        var serviceType = factoryMarker?.TypeArguments.FirstOrDefault();
        var constructor = classSymbol.Constructors.FirstOrDefault();

        if (serviceType is null || constructor is null) return null;


        var requiredProperties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod is not null)
            .Where(x => x.IsRequired);


        return new ServiceFactoryModel(
            serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            constructor.Parameters
                .Select(x =>
                    new ConstructorArgument(x.Type.ToDisplayString(),
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList(),
            requiredProperties.Select(x =>
                    new RequiredProperty(x.Type.ToDisplayString(), x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList()
        );
    }

    internal static EntrypointFactoryModel? ExtractEntrypointFactoryModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryMarker(x) is not null);
        if (factoryAttribute is null) return null;

        var factoryMarker = FindFactoryMarker(factoryAttribute);
        var baseType = factoryMarker?.TypeArguments.FirstOrDefault();
        var constructor = classSymbol.Constructors.FirstOrDefault();

        if (baseType is null || constructor is null) return null;

        var requiredProperties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod is not null)
            .Where(x => x.IsRequired);

        return new EntrypointFactoryModel(
            baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            constructor.Parameters
                .Select(x =>
                    new ConstructorArgument(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList(),
            requiredProperties.Select(x =>
                    new RequiredProperty(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList()
        );
    }

    internal static KeyedFactoryModel? ExtractKeyedFactoryModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryMarker(x) is not null);
        if (factoryAttribute is null) return null;

        var factoryMarker = FindFactoryMarker(factoryAttribute);
        var baseType = factoryMarker?.TypeArguments.FirstOrDefault();
        var constructor = classSymbol.Constructors.FirstOrDefault();

        if (baseType is null || constructor is null) return null;

        // Extract key information from the attribute
        var keyInfo = ExtractKeyInfo(factoryAttribute, classSymbol);
        if (keyInfo is null) return null; // Key is required for KeyedFactory

        var requiredProperties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod is not null)
            .Where(x => x.IsRequired);

        return new KeyedFactoryModel(
            baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            constructor.Parameters
                .Select(x =>
                    new ConstructorArgument(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList(),
            requiredProperties.Select(x =>
                    new RequiredProperty(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList(),
            keyInfo
        );
    }

    internal static KeyInfo? ExtractKeyInfo(AttributeData factoryAttribute, INamedTypeSymbol classSymbol)
    {
        if (factoryAttribute.AttributeClass is not { } attClass) return null;

        //Only Named Arguments are considered to be used for key definition
        foreach (var kvp in factoryAttribute.NamedArguments)
        {
            var name = kvp.Key;
            var value = kvp.Value;

            var property = attClass.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault();
            if (property is null) continue;

            var keyType = DetermineKeyType(property);

            switch (keyType)
            {
                case KeyType.None: continue;
                case KeyType.Direct: return new DirectKeyInfo(value.Value?.ToString() ?? "");
                case KeyType.Property:
                {
                    var propertyName = value.Value?.ToString();
                    if (propertyName is null) continue;

                    var keyProperty = classSymbol.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
                    if (keyProperty is null) continue;

                    return new PropertyKeyInfo(value.Value?.ToString() ?? "",
                        keyProperty.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
        }

        return null;
    }

    internal static bool RenderEntrypointFactory(EntrypointFactoryModel model, out string code, out string fileName)
    {
        fileName = $"{model.ImplementationTypeName}_EntrypointFactory.g.cs";

        return FluidHelper.TryRenderTemplate("DI.EntrypointFactory.liquid", model, out code);
    }

    internal static bool RenderKeyedFactory(KeyedFactoryModel model, out string code, out string fileName)
    {
        fileName = $"{model.ImplementationTypeName}_KeyedFactory.g.cs";

        return FluidHelper.TryRenderTemplate("DI.KeyedFactory.liquid", model, out code);
    }
}