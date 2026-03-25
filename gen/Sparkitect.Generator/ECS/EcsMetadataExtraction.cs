using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.Metadata;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.ECS;

/// <summary>
/// Extraction logic for the ECS resolution metadata pipeline.
/// Scans <c>MethodDeclarationSyntax</c> for methods with SF-derived attributes
/// (e.g., <c>[EcsSystemFunction]</c>) and extracts <c>[ComponentQuery]</c>-typed
/// parameters to build <see cref="EcsSystemMetadataModel"/> instances.
/// </summary>
public static class EcsMetadataExtraction
{
    private const string StatelessFunctionAttributeBase = "Sparkitect.Stateless.StatelessFunctionAttribute";
    private const string ComponentQueryAttributeFqn = "Sparkitect.ECS.Queries.ComponentQueryAttribute";

    /// <summary>
    /// Transform function for <c>CreateSyntaxProvider</c>. Discovers methods with SF-derived
    /// attributes, extracts query-typed parameters, and returns the model or null for
    /// invalid/skipped targets.
    /// </summary>
    public static EcsSystemMetadataModel? TryExtractEcsSystemMetadata(
        GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.Node is not MethodDeclarationSyntax)
            return null;

        var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) as IMethodSymbol;
        if (methodSymbol is null)
            return null;

        // Find SF-derived attribute on the method
        string? identifier = null;
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (MetadataExtractionPipeline.InheritsFrom(attr.AttributeClass, StatelessFunctionAttributeBase))
            {
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string id)
                {
                    identifier = id;
                }
                break;
            }
        }

        if (string.IsNullOrEmpty(identifier))
            return null;

        // Compute wrapper type name using the SF naming convention
        var identifierPascal = StringCase.ToPascalCase(identifier!);
        var wrapperClassName = $"{identifierPascal}Func";

        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
            return null;

        var parentFullName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var wrapperFullTypeName = $"{parentFullName}.{wrapperClassName}";

        // Strip global:: prefix
        if (wrapperFullTypeName.StartsWith("global::"))
            wrapperFullTypeName = wrapperFullTypeName.Substring(8);

        // Extract wrapper namespace
        var lastDot = wrapperFullTypeName.LastIndexOf('.');
        var wrapperNs = lastDot >= 0 ? wrapperFullTypeName.Substring(0, lastDot) : string.Empty;

        // Scan parameters for [ComponentQuery]-typed types
        var queryParams = new ImmutableValueArray<QueryParameterInfo>.Builder();

        foreach (var param in methodSymbol.Parameters)
        {
            ct.ThrowIfCancellationRequested();

            if (param.Type is not INamedTypeSymbol paramType)
                continue;

            var isComponentQuery = paramType.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == ComponentQueryAttributeFqn);

            if (isComponentQuery)
            {
                queryParams.Add(new QueryParameterInfo(
                    paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
        }

        // If no query parameters found, nothing to emit
        if (queryParams.Count == 0)
            return null;

        return new EcsSystemMetadataModel(
            wrapperFullTypeName,
            wrapperNs,
            queryParams.ToImmutableValueArray());
    }
}
