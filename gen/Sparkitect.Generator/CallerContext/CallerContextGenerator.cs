using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Sparkitect.Generator.CallerContext;
#pragma warning disable RSEXPERIMENTAL002

/// <summary>
/// Source generator that creates method interceptors for methods with [InjectCallerContext] parameters
/// to automatically inject CallerContext with file path and line number at the call site.
/// </summary>
[Generator]
public class CallerContextGenerator : IIncrementalGenerator
{
    public const string InjectCallerContextAttribute = "Sparkitect.Utils.InjectCallerContextAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        var methodInvocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return node is InvocationExpressionSyntax;
                },
                (ctx, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var invocation = (InvocationExpressionSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetSymbolInfo(invocation, token).Symbol;
                    if (symbol is not IMethodSymbol methodSymbol)
                    {
                        return ((IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)?)null;
                    }

                    // Check if any parameter has [InjectCallerContext] attribute
                    var hasInjectCallerContextParam = methodSymbol.Parameters.Any(p =>
                        p.GetAttributes().Any(a =>
                            a.AttributeClass?.ToDisplayString() == InjectCallerContextAttribute));

                    if (!hasInjectCallerContextParam)
                    {
                        return ((IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)?)null;
                    }

                    if (ctx.SemanticModel.GetOperation(invocation) is not IInvocationOperation invocationOperation)
                    {
                        return ((IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)?)null;
                    }

                    var classSyntax = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    if (classSyntax is null)
                    {
                        return ((IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)?)null;
                    }

                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classSyntax);
                    if (classSymbol is not INamedTypeSymbol containingClass)
                    {
                        return ((IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)?)null;
                    }

                    return (invocationOperation, methodSymbol, containingClass);
                }
            ).Where(x => x is not null)
            .Select((x, _) => x!.Value)
            .Collect()
            .SelectMany((x, _) => x.GroupBy(t => t.containingClass, SymbolEqualityComparer.Default));

        context.RegisterImplementationSourceOutput(
            methodInvocationProvider.Combine(buildSettings),
            ProcessCallerContextInvocation
        );
    }

    private void ProcessCallerContextInvocation(SourceProductionContext sourceProductionContext,
        (IGrouping<ISymbol?, (IInvocationOperation invocationOperation, IMethodSymbol methodSymbol, INamedTypeSymbol containingClass)> Left, ModBuildSettings Right) valueTuple)
    {
        var classSymbol = valueTuple.Left.Key as INamedTypeSymbol;
        var invocations = valueTuple.Left.Select(x => (x.invocationOperation, x.methodSymbol)).ToList();
        var buildSettings = valueTuple.Right;
        var interceptorNamespace = $"{buildSettings.RootNamespace}.CallerContext";

        var fullClassName = $"{classSymbol!.ToDisplayString().Replace('.', '_').Replace('<', '_').Replace('>', '_')}_CallerContextInjector";

        var model = new
        {
            fullClassName,
            interceptorNamespace,
            interceptions = invocations.Select(x =>
            {
                var interceptLocationAttribute = GetInterceptLocationAtt(x.invocationOperation, out var filePath, out var lineNumber);

                // Get all parameters including CallerContext (interceptor must match method signature exactly)
                var allParams = x.methodSymbol.Parameters
                    .Select(y => new
                    {
                        type = y.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        name = y.Name,
                        refKind = y.RefKind switch
                        {
                            RefKind.In => "in ",
                            RefKind.Ref => "ref ",
                            RefKind.Out => "out ",
                            RefKind.RefReadOnlyParameter => "ref readonly ",
                            _ => ""
                        },
                        isCallerContext = y.GetAttributes().Any(a =>
                            a.AttributeClass?.ToDisplayString() == InjectCallerContextAttribute)
                    })
                    .ToList();

                // Get parameters excluding the CallerContext parameter (for method call)
                var nonCallerContextParams = allParams.Where(p => !p.isCallerContext).ToList();

                return new
                {
                    targetClass = x.methodSymbol.ContainingType.ToDisplayString(),
                    targetMethod = x.methodSymbol.Name,
                    staticCall = x.methodSymbol.IsStatic,
                    returnType = x.methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    allParameters = allParams,
                    parameters = nonCallerContextParams,
                    interceptLocationAttribute,
                    filePath,
                    lineNumber
                };
            }).ToList()
        };

        if (!FluidHelper.TryRenderTemplate("CallerContext.CallerContextInjector.liquid", model, out var code))
        {
            return;
        }

        sourceProductionContext.AddSource(
            $"{fullClassName}.g.cs",
            code
        );
    }

    private static string GetInterceptLocationAtt(IInvocationOperation operation, out string filePath, out int lineNumber)
    {
        filePath = "";
        lineNumber = 0;

        var semantic = operation.SemanticModel;
        if (semantic is null) return "";
        if (operation.Syntax is not InvocationExpressionSyntax syntax) return "";
        if (Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptableLocation(semantic, syntax) is not { } location) return "";

        var attribute = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptsLocationAttributeSyntax(location);

        // Parse file path and line number from the attribute
        // Attribute format: [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "base64path")]
        // or similar format - we need to extract from the location itself
        var lineSpan = syntax.GetLocation().GetLineSpan();
        filePath = lineSpan.Path;
        lineNumber = lineSpan.StartLinePosition.Line + 1; // Convert from 0-indexed to 1-indexed

        return attribute;
    }
}
