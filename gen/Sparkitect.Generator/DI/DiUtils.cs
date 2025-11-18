using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sparkitect.Generator.DI;

/// <summary>
/// Utility class providing helper methods and constants for DI factory code generation.
/// Contains logic for analyzing factory attributes, determining key types, and validating type symbols.
/// </summary>
public static class DiUtils
{
    internal const string GeneratorAttributeNamespace = "Sparkitect.DI.GeneratorAttributes";
    internal const string FactoryMarkerInterface = $"{GeneratorAttributeNamespace}.IFactoryMarker";
    
    internal const string KeyAttribute = $"{GeneratorAttributeNamespace}.KeyAttribute";
    internal const string KeyPropertyAttribute = $"{GeneratorAttributeNamespace}.KeyPropertyAttribute";
    
    internal const string FactoryTypeAttribute = $"{GeneratorAttributeNamespace}.FactoryGenerationTypeAttribute";
    internal const string FactoryGenerationType = $"{GeneratorAttributeNamespace}.FactoryGenerationType";
    
    internal const string SingletonAttributeMetadataName = $"{GeneratorAttributeNamespace}.SingletonAttribute`1";
    
    internal static INamedTypeSymbol? FindFactoryMarker(AttributeData attributeData)
    {
        var attributeClass = attributeData.AttributeClass;
        if (attributeClass is null) return null;
        
        // Check if the attribute class implements IFactoryMarker<T>
        foreach (var iface in attributeClass.AllInterfaces)
        {
            var displayString = iface.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType);
            if (displayString == FactoryMarkerInterface)
            {
                return iface;
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
    Factory
}
