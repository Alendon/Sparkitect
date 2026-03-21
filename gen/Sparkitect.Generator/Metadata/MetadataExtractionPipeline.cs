using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Metadata;

/// <summary>
/// Shared extraction toolbox for metadata attribute extraction from Roslyn symbols.
/// Generalizes the proven extraction logic from StatelessFunctionGenerator to work on
/// any ISymbol (methods, types, etc.) for universal metadata infrastructure.
/// </summary>
public static class MetadataExtractionPipeline
{
    /// <summary>
    /// Core extraction -- works on IMethodSymbol or INamedTypeSymbol.
    /// Analyzes the constructor parameters of <paramref name="metadataType"/> and matches
    /// attributes from <paramref name="targetSymbol"/> to each parameter by non-generic base type name.
    /// </summary>
    /// <param name="metadataType">The metadata class whose constructor params we match.</param>
    /// <param name="targetSymbol">The symbol whose attributes we scan (method or type).</param>
    /// <param name="typeArgumentResolver">Optional callback for ErrorTypeSymbol resolution.
    /// SF passes its Func deduction for wrapper types; others pass null for default display string behavior.</param>
    /// <returns>Extracted constructor parameters with matched attribute instances.</returns>
    public static ImmutableValueArray<MetadataConstructorParam> Extract(
        INamedTypeSymbol metadataType,
        ISymbol targetSymbol,
        Func<ITypeSymbol, ISymbol, string>? typeArgumentResolver = null)
    {
        var builder = new ImmutableValueArray<MetadataConstructorParam>.Builder();

        // Get single constructor
        var ctor = metadataType.Constructors.FirstOrDefault(c => !c.IsStatic);
        if (ctor is null)
            return builder.ToImmutableValueArray();

        foreach (var ctorParam in ctor.Parameters)
        {
            var paramType = ctorParam.Type;
            bool isArray = paramType is IArrayTypeSymbol;
            bool isNullable = paramType.NullableAnnotation == NullableAnnotation.Annotated;

            // Get element type if array
            var elementType = isArray ? ((IArrayTypeSymbol)paramType).ElementType : paramType;

            // Get non-generic base for matching (strip nullable)
            var baseTypeName = GetNonGenericBaseTypeName(elementType);

            // Match target symbol attributes to this param type
            var instances = new ImmutableValueArray<MetadataAttributeInstance>.Builder();
            foreach (var attr in targetSymbol.GetAttributes())
            {
                var attrBaseName = GetNonGenericBaseTypeName(attr.AttributeClass);
                if (attrBaseName == baseTypeName)
                {
                    // Extract generic args
                    var genericArgs = new ImmutableValueArray<string>.Builder();
                    if (attr.AttributeClass is { IsGenericType: true })
                    {
                        foreach (var typeArg in attr.AttributeClass.TypeArguments)
                        {
                            var resolvedTypeName = typeArgumentResolver != null
                                ? typeArgumentResolver(typeArg, targetSymbol)
                                : typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            genericArgs.Add(resolvedTypeName);
                        }
                    }

                    // Extract constructor args
                    var ctorArgs = new ImmutableValueArray<string>.Builder();
                    foreach (var arg in attr.ConstructorArguments)
                    {
                        ctorArgs.Add(FormatTypedConstant(arg));
                    }

                    instances.Add(new MetadataAttributeInstance(
                        genericArgs.ToImmutableValueArray(),
                        ctorArgs.ToImmutableValueArray()));
                }
            }

            var attrTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // Strip generic suffix for base type name in template
            if (elementType is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                attrTypeName = namedType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Remove `N suffix
                var backtickIdx = attrTypeName.IndexOf('`');
                if (backtickIdx > 0)
                    attrTypeName = attrTypeName.Substring(0, backtickIdx);
            }

            builder.Add(new MetadataConstructorParam(
                attrTypeName,
                isNullable,
                isArray,
                instances.ToImmutableValueArray()));
        }

        return builder.ToImmutableValueArray();
    }

    /// <summary>
    /// Gets the non-generic base type name for matching, stripping nullable wrappers and generic suffixes.
    /// </summary>
    public static string GetNonGenericBaseTypeName(ITypeSymbol? type)
    {
        if (type is null) return string.Empty;

        // Handle nullable
        if (type.NullableAnnotation == NullableAnnotation.Annotated && type is INamedTypeSymbol nullable)
        {
            type = nullable.TypeArguments.FirstOrDefault() ?? type;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            return named.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType);
        }

        return type.ToDisplayString(DisplayFormats.NamespaceAndType);
    }

    /// <summary>
    /// Formats a TypedConstant as a C# literal string suitable for source generation output.
    /// </summary>
    public static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.IsNull) return "null";

        return constant.Kind switch
        {
            TypedConstantKind.Primitive when constant.Value is string s => $"\"{s}\"",
            TypedConstantKind.Primitive when constant.Value is bool b => b ? "true" : "false",
            TypedConstantKind.Primitive => constant.Value?.ToString() ?? "null",
            TypedConstantKind.Enum => $"({constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){constant.Value}",
            _ => constant.ToCSharpString()
        };
    }

    /// <summary>
    /// Checks if a type inherits from a base type by walking the type hierarchy.
    /// </summary>
    public static bool InheritsFrom(INamedTypeSymbol? type, string baseTypeName)
    {
        while (type is not null)
        {
            if (type.ToDisplayString(DisplayFormats.NamespaceAndType) == baseTypeName)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Finds a generic base type in the inheritance chain matching the given non-generic base name.
    /// </summary>
    public static INamedTypeSymbol? FindGenericBase(INamedTypeSymbol? type, string genericBaseName)
    {
        while (type is not null)
        {
            if (type.IsGenericType &&
                type.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType.WithGenericsOptions(SymbolDisplayGenericsOptions.None)) == genericBaseName)
                return type;
            type = type.BaseType;
        }
        return null;
    }
}
