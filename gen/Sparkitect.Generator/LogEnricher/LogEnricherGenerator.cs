using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Sparkitect.Generator.LogEnricher;
#pragma warning disable RSEXPERIMENTAL002

/// <summary>
/// Source generator that creates method interceptors for Serilog logging methods to automatically
/// enrich log context with ModName and Class information.
/// </summary>
[Generator]
public class LogEnricherGenerator : IIncrementalGenerator
{
    public const string LogMethodMarkerAttribute = "Serilog.Core.MessageTemplateFormatMethodAttribute";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compActiveProvider = context.AnalyzerConfigOptionsProvider.Select(
            (x, _) =>
            {
                if (x.GlobalOptions.TryGetValue("build_property.DisableLogEnrichmentGenerator", out var value))
                {
                    return value.ToLowerInvariant() != "true";
                }

                return true;
            });

        var modNameProvider = context.AnalyzerConfigOptionsProvider.Select(
            (x, _) => x.GlobalOptions.TryGetValue("build_property.ModName", out var value) ? value : string.Empty);
        
        var logMethodInvocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return node is InvocationExpressionSyntax;
                },
                (context, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, invocation, token).Symbol;
                    if (symbol is not IMethodSymbol methodSymbol)
                    {
                        return null;
                    }

                    var hasAttribute = methodSymbol.GetAttributes().Any(x =>
                        x.AttributeClass?.ToDisplayString() == LogMethodMarkerAttribute);

                    if (context.SemanticModel.GetOperation(invocation) is not IInvocationOperation invocationSymbol)
                    {
                        return null;
                    }
                    
                    var classSyntax = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    if(classSyntax is null)
                    {
                        return null;
                    }

                    var classSymbol = context.SemanticModel.GetSymbolInfo(classSyntax);
                    if (classSymbol.Symbol is not INamedTypeSymbol containingClass)
                    {
                        return null;
                    }

                    return hasAttribute
                        ? (invocationSymbol, methodSymbol, containingClass)
                        : ((IInvocationOperation invocationSymbol, IMethodSymbol methodSymbol, INamedTypeSymbol
                            containingClass)?)null;
                }
            ).Where(x => x is not null)
            .Select((x, _) => x!.Value)
            .Collect()
            .SelectMany((x, _) => x.GroupBy(t => t.containingClass, SymbolEqualityComparer.Default));


        context.RegisterImplementationSourceOutput(
            logMethodInvocationProvider.Combine(compActiveProvider.Combine(modNameProvider)),
            ProcessLogMethodInvocation
        );
    }

    private void ProcessLogMethodInvocation(SourceProductionContext sourceProductionContext,
        (IGrouping<ISymbol, (IInvocationOperation invocationSymbol, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)> Left, (bool Left, string Right) Right) tuple)
    {
        var classSymbol = tuple.Left.Key;
        var invocations = tuple.Left.Select(x => (x.invocationSymbol, x.methodSymbol)).ToList();
        var (compActive, modName) = tuple.Right;

        if (!compActive)
        {
            return;
        }

        var className = $"{classSymbol.ToDisplayString()}_Enricher";

        var model = new
        {
            className,
            modName,
            interceptions = invocations.Select(x => new
            {
                targetClass = x.methodSymbol.ContainingType.ToDisplayString(),
                targetMethod = x.methodSymbol.Name,
                staticCall = x.methodSymbol.IsStatic,
                parameters = x.methodSymbol.Parameters.Select(y => new
                {
                    type = y.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    name = y.Name
                })
            })
        };

        if (!FluidHelper.TryRenderTemplate("LogEnricher.LogEnricher.cs.liquid", model, out var code))
        {
            return;
        }
        
        sourceProductionContext.AddSource(
            $"{className}.g.cs",
            code
        );
        
    }

}