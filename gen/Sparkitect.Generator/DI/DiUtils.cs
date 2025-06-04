using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sparkitect.Generator.DI;

public static class DiUtils
{
    internal const string GeneratorAttributeNamespace = "Sparkitect.DI.GeneratorAttributes";
    internal const string FactoryBaseAttribute = $"{GeneratorAttributeNamespace}.FactoryAttribute";
    
    internal const string KeyAttribute = $"{GeneratorAttributeNamespace}.KeyAttribute";
    internal const string KeyPropertyAttribute = $"{GeneratorAttributeNamespace}.KeyPropertyAttribute";
    
    internal const string FactoryTypeAttribute = $"{GeneratorAttributeNamespace}.FactoryGenerationTypeAttribute";
    internal const string FactoryGenerationType = $"{GeneratorAttributeNamespace}.FactoryGenerationType";
    
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

    internal static FactoryType GetFactoryGenerationType(INamedTypeSymbol? attributeClass)
    {
        var attributes = attributeClass?.GetAttributes();

        var generationTypeAtt = attributes?.FirstOrDefault(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == FactoryTypeAttribute);

        if (generationTypeAtt is null)
            return FactoryType.None;

        var enumString = generationTypeAtt.ConstructorArguments[0].ToCSharpString();
        
        return enumString switch
        {
            "Sparkitect.DI.GeneratorAttributes.FactoryGenerationType.Service" => FactoryType.Service,
            "Sparkitect.DI.GeneratorAttributes.FactoryGenerationType.Factory" => FactoryType.Factory,
            "Sparkitect.DI.GeneratorAttributes.FactoryGenerationType.Entrypoint" => FactoryType.Entrypoint,
            _ => FactoryType.None
        };
    }
    
    internal static bool IsAbstractOrInterface(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface || type.IsAbstract;
    }

    internal static KeyType DetermineKeyType(IPropertySymbol property)
    {
        var attributes = property.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyAttribute)
                return KeyType.Direct;

            if (attribute.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == KeyPropertyAttribute)
                return KeyType.Property;
        }

        return KeyType.None;
    }
    
    
}

enum KeyType
{
    None,
    Direct,
    Property
}

enum FactoryType
{
    None,
    Service,
    Factory,
    Entrypoint
}