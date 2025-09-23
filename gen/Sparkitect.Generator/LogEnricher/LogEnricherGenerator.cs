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
        var buildSettings = context.GetModBuildSettings();
        
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
                    var symbol = context.SemanticModel.GetSymbolInfo(invocation, token).Symbol;
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
                    if (classSyntax is null)
                    {
                        return null;
                    }

                    var classSymbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);
                    if (classSymbol is not INamedTypeSymbol containingClass)
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
            logMethodInvocationProvider.Combine(buildSettings),
            ProcessLogMethodInvocation
        );
    }

    private void ProcessLogMethodInvocation(SourceProductionContext sourceProductionContext,
        (IGrouping<ISymbol?, (IInvocationOperation invocationSymbol, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)> Left, ModBuildSettings Right) valueTuple)
    {
        var classSymbol = valueTuple.Left.Key as INamedTypeSymbol;
        var invocations = valueTuple.Left.Select(x => (x.invocationSymbol, x.methodSymbol)).ToList();
        var buildSettings = valueTuple.Right;
        //TODO base the namespace on the SgOutputNamespace option
        var interceptorNamespace = $"{buildSettings.RootNamespace}.LogEnricher";

        if (!buildSettings.EnableLogEnrichment)
        {
            return;
        }
        
        var fullClassName = $"{classSymbol!.ToDisplayString().Replace('.', '_').Replace('<', '_').Replace('>', '_')}_LogEnricher";
        var className = classSymbol.Name;

        var model = new
        {
            className,
            fullClassName,
            modName = buildSettings.ModName,
            interceptorNamespace,
            interceptions = invocations.Select(x =>
            {
                return new
                {
                    targetClass = x.methodSymbol.ContainingType.ToDisplayString(),
                    targetMethod = x.methodSymbol.Name,
                    staticCall = x.methodSymbol.IsStatic,
                    parameters = x.methodSymbol.Parameters.Select(y => new
                    {
                        type = y.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        name = y.Name
                    }),
                    interceptLocationAttribute = GetInterceptLocationAtt(x.invocationSymbol)
                };

                string GetInterceptLocationAtt(IInvocationOperation symbol)
                {
                    var semantic = symbol.SemanticModel;
                    if (symbol.Syntax is not InvocationExpressionSyntax syntax) return "";
                    if (Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptableLocation(semantic, syntax) is not { } location) return "";
                    return Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptsLocationAttributeSyntax(location);
                }
            })
        };

        if (!FluidHelper.TryRenderTemplate("LogEnricher.LogEnricher.liquid", model, out var code))
        {
            return;
        }

        sourceProductionContext.AddSource(
            $"{fullClassName}.g.cs",
            code
        );
    }
}