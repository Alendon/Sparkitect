using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.Metadata;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.ECS;

/// <summary>
/// Attribute extraction logic for <c>[ComponentQuery]</c> partial classes.
/// Serves as the transform function for <see cref="EcsQueryGenerator"/>'s
/// <c>CreateSyntaxProvider</c>.
/// </summary>
public static class EcsQueryExtraction
{
    private const string ComponentQueryAttributeFqn = "Sparkitect.ECS.Queries.ComponentQueryAttribute";
    private const string CompilerGeneratedAttributeFqn = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    private const string ReadComponentsPrefix = "Sparkitect.ECS.Queries.ReadComponents";
    private const string WriteComponentsPrefix = "Sparkitect.ECS.Queries.WriteComponents";
    private const string ExcludeComponentsPrefix = "Sparkitect.ECS.Queries.ExcludeComponents";
    private const string ExposeKeyAttributePrefix = "Sparkitect.ECS.Queries.ExposeKeyAttribute";

    /// <summary>
    /// Transform function for <c>CreateSyntaxProvider</c>. Discovers <c>[ComponentQuery]</c>
    /// partial classes, extracts component access and <c>[ExposeKey]</c> attributes, validates
    /// no cross-family overlaps, and returns the model or null for invalid/skipped targets.
    /// </summary>
    public static EcsQueryModel? TryExtractQueryModel(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.Node is not ClassDeclarationSyntax)
            return null;

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) as INamedTypeSymbol;
        if (symbol is null)
            return null;

        // Pattern 5: Skip compiler-generated types to prevent infinite SG loops
        if (symbol.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                CompilerGeneratedAttributeFqn))
            return null;

        // Check for [ComponentQuery] attribute
        bool hasComponentQuery = false;
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == ComponentQueryAttributeFqn)
            {
                hasComponentQuery = true;
                break;
            }
        }

        if (!hasComponentQuery)
            return null;

        // Extract component access attributes
        var readBuilder = new ImmutableValueArray<ComponentInfo>.Builder();
        var writeBuilder = new ImmutableValueArray<ComponentInfo>.Builder();
        var excludeBuilder = new ImmutableValueArray<ComponentInfo>.Builder();
        var isKeyed = false;
        string? keyTypeFqn = null;
        string? keyTypeShort = null;
        var keyRequired = false;

        foreach (var attr in symbol.GetAttributes())
        {
            ct.ThrowIfCancellationRequested();

            if (attr.AttributeClass is not { IsGenericType: true } attrClass)
                continue;

            var baseName = MetadataExtractionPipeline.GetNonGenericBaseTypeName(attrClass);

            // Component access attribute families
            if (baseName == ReadComponentsPrefix)
            {
                foreach (var typeArg in attrClass.TypeArguments)
                {
                    readBuilder.Add(new ComponentInfo(
                        typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        typeArg.Name));
                }
            }
            else if (baseName == WriteComponentsPrefix)
            {
                foreach (var typeArg in attrClass.TypeArguments)
                {
                    writeBuilder.Add(new ComponentInfo(
                        typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        typeArg.Name));
                }
            }
            else if (baseName == ExcludeComponentsPrefix)
            {
                foreach (var typeArg in attrClass.TypeArguments)
                {
                    excludeBuilder.Add(new ComponentInfo(
                        typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        typeArg.Name));
                }
            }
            else if (baseName == ExposeKeyAttributePrefix)
            {
                isKeyed = true;
                var keyType = attrClass.TypeArguments[0];
                keyTypeFqn = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                keyTypeShort = keyType.Name;
                keyRequired = (bool)attr.ConstructorArguments[0].Value!;
            }
        }

        // Validate: no component in multiple families (D-12 silent drop)
        if (HasCrossFamilyOverlap(readBuilder, writeBuilder) ||
            HasCrossFamilyOverlap(readBuilder, excludeBuilder) ||
            HasCrossFamilyOverlap(writeBuilder, excludeBuilder))
            return null;

        // Must have at least one read or write component
        if (readBuilder.Count == 0 && writeBuilder.Count == 0)
            return null;

        return new EcsQueryModel(
            symbol.ContainingNamespace.ToDisplayString(),
            symbol.Name,
            readBuilder.ToImmutableValueArray(),
            writeBuilder.ToImmutableValueArray(),
            excludeBuilder.ToImmutableValueArray(),
            isKeyed,
            keyTypeFqn,
            keyTypeShort,
            keyRequired);
    }

    /// <summary>
    /// Checks whether any component appears in both lists (by FullyQualifiedName).
    /// </summary>
    private static bool HasCrossFamilyOverlap(
        ImmutableValueArray<ComponentInfo>.Builder listA,
        ImmutableValueArray<ComponentInfo>.Builder listB)
    {
        for (int i = 0; i < listA.Count; i++)
        {
            for (int j = 0; j < listB.Count; j++)
            {
                if (listA[i].FullyQualifiedName == listB[j].FullyQualifiedName)
                    return true;
            }
        }
        return false;
    }
}
