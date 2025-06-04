using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparkitect.Generator.DI;

[Generator]
public class DiFactoryGenerator : IIncrementalGenerator
{
    /*
     * TODO Write Analyzer
     * Only abstract class/interfaces as dependencies => Warning/Error
     * Only one constructor => Error
     * Required Properties should be init only => Warning
     * Only 1 Generation Marker per Type => Warning
     * No Conflicting Generation Marker per Type => Error
     * Factory type enum declaration must be a constant value living in the attribute inheritance chain
     * For KeyedFactory: Exactly one key association (either [Key] or [KeyProperty]) must be set => Error
     * For KeyedFactory: Key parameter must be string type (not Identification or OneOf) => Error
     * For KeyedFactory: KeyProperty must reference a valid static property => Error
     */

    const string FactoryBaseAttribute = "Sparkitect.DI.GeneratorAttributes.FactoryAttribute";
    const string FactoryTypeAttribute = "Sparkitect.DI.GeneratorAttributes.FactoryGenerationTypeAttribute";
    const string KeyAttribute = "Sparkitect.DI.GeneratorAttributes.KeyAttribute";
    const string KeyPropertyAttribute = "Sparkitect.DI.GeneratorAttributes.KeyPropertyAttribute";

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

                var factoryAttribute = attributes.FirstOrDefault(x => FindFactoryBase(x) is not null);
                if (factoryAttribute is null) return null;
                
                var factoryType = factoryAttribute.AttributeClass?.GetAttributes().FirstOrDefault(x =>
                        x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == FactoryTypeAttribute)?
                    .ConstructorArguments.FirstOrDefault().ToCSharpString()
                    .Split('.').LastOrDefault();

                if (factoryType is null) return null;


                switch (factoryType)
                {
                    case "Service":
                    {
                        return ExtractSingletonModelData(classSymbol);
                    }
                    case "Factory":
                    {
                        return ExtractKeyedFactoryModelData(classSymbol);
                    }
                    case "Entrypoint":
                    {
                        return ExtractEntrypointFactoryModelData(classSymbol);
                    }
                    default: return null;
                }
            }).NotNull();

        context.RegisterSourceOutput(singletonModelProvider, (context, model) =>
        {
            string? code;
            string? fileName;
            
            switch (model)
            {
                case SingletonModel singletonModel:
                    if (RenderSingletonFactory(singletonModel, out code, out fileName))
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

    internal static INamedTypeSymbol? FindFactoryBase(AttributeData attributeData)
    {
        var attributeClass = attributeData.AttributeClass;

        while (attributeClass is not null)
        {
            var displayString = attributeClass.ToDisplayString(DisplayFormats.NamespaceAndType);

            switch (displayString)
            {
                case FactoryBaseAttribute:
                    return attributeClass;
                case "System.Attribute":
                    return null;
                default:
                    attributeClass = attributeClass.BaseType;
                    break;
            }
        }

        return null;
    }

    internal static bool RenderSingletonFactory(SingletonModel model, out string code, out string fileName)
    {
        fileName = $"{model.ImplementationTypeName}_Factory.g.cs";

        return FluidHelper.TryRenderTemplate("DI.SingletonFactory.liquid", model, out code);
    }

    internal static SingletonModel? ExtractSingletonModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryBase(x) is not null);
        if (factoryAttribute is null) return null;
        
        var factoryBase = FindFactoryBase(factoryAttribute);
        var serviceType = factoryBase?.TypeArguments.FirstOrDefault();
        var constructor = classSymbol.Constructors.FirstOrDefault();
        
        if (serviceType is null || constructor is null) return null;


        var requiredProperties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod is not null)
            .Where(x => x.IsRequired);


        return new SingletonModel(
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
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryBase(x) is not null);
        if (factoryAttribute is null) return null;
        
        var factoryBase = FindFactoryBase(factoryAttribute);
        var baseType = factoryBase?.TypeArguments.FirstOrDefault();
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
                    new RequiredProperty(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList()
        );
    }
    
    internal static KeyedFactoryModel? ExtractKeyedFactoryModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes().FirstOrDefault(x => FindFactoryBase(x) is not null);
        if (factoryAttribute is null) return null;
        
        var factoryBase = FindFactoryBase(factoryAttribute);
        var baseType = factoryBase?.TypeArguments.FirstOrDefault();
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
                    new RequiredProperty(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToValueCompareList(),
            keyInfo
        );
    }
    
    internal static KeyInfo? ExtractKeyInfo(AttributeData factoryAttribute, INamedTypeSymbol classSymbol)
    {
        // Get the attribute constructor
        var attributeConstructor = factoryAttribute.AttributeConstructor;
        if (attributeConstructor is null) return null;
        
        // Ensure we have the same number of parameters and arguments
        if (attributeConstructor.Parameters.Length != factoryAttribute.ConstructorArguments.Length) return null;
        
        // Find constructor parameters with Key or KeyProperty attributes
        var keyParameters = attributeConstructor.Parameters
            .Select((param, index) => new { Parameter = param, Index = index, Value = factoryAttribute.ConstructorArguments[index] })
            .Where(x => x.Parameter.GetAttributes().Any(attr => 
                attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyAttribute || 
                attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyPropertyAttribute))
            .ToList();
        
        // Take the first parameter that has a non-null, non-empty string value
        var keyParam = keyParameters.FirstOrDefault(x => x.Value.Value is string s && !string.IsNullOrEmpty(s));
        if (keyParam is null) return null;
        
        // Check if it's a Key or KeyProperty attribute
        var hasKeyAttribute = keyParam.Parameter.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyAttribute);
        
        if (hasKeyAttribute && keyParam.Value.Value is string directKey)
        {
            return new DirectKeyInfo(directKey);
        }
        
        var hasKeyPropertyAttribute = keyParam.Parameter.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyPropertyAttribute);
        
        if (hasKeyPropertyAttribute && keyParam.Value.Value is string propertyName)
        {
            // Try to find the property on the class to determine its return type
            var property = classSymbol.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault(p => p.IsStatic);
            if (property is null) return null; // Property must exist
            
            var returnType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new PropertyKeyInfo(propertyName, returnType);
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