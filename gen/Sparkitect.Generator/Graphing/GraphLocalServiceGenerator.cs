using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;

namespace Sparkitect.Generator.Graphing;

[Generator]
public class GraphLocalServiceGenerator : IIncrementalGenerator
{
    private const string GraphLocalAttributeMetadataName =
        "Sparkitect.Graphing.GraphLocalAttribute`2";
    private const string GraphLocalAttributeName =
        "Sparkitect.Graphing.GraphLocalAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        var graphLocalsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            GraphLocalAttributeMetadataName,
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol classSymbol)
                    return null;

                var attr = classSymbol.GetAttributes()
                    .FirstOrDefault(a =>
                        a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                            == GraphLocalAttributeName);
                if (attr?.AttributeClass?.TypeArguments.Length != 2) return null;

                var interfaceFqn = attr.AttributeClass.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var graphBaseFqn = attr.AttributeClass.TypeArguments[1]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var factory = DiPipeline.ExtractFactory(classSymbol, new FactoryIntent.Service(), interfaceFqn);
                if (factory is null) return null;

                var registration = DiPipeline.ToRegistration(factory, classSymbol);

                return new GraphLocalData(
                    new FactoryWithRegistration(factory, registration),
                    InterfaceFqn: interfaceFqn,
                    GraphBaseFqn: graphBaseFqn,
                    ImplFqn: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ImplSimpleName: classSymbol.Name,
                    ImplNamespace: classSymbol.ContainingNamespace.ToDisplayString());
            }).NotNull();

        // Per-class _Factory.g.cs via the shared DiPipeline (template unchanged).
        context.RegisterSourceOutput(graphLocalsProvider, (ctx, data) =>
        {
            if (DiPipeline.RenderFactory(data.FactoryData.Factory, out var code, out var fileName))
                ctx.AddSource(fileName, code);
        });

        // Per-class core-configurator entrypoint via the shared DI pipeline (Configurator.liquid).
        context.RegisterSourceOutput(graphLocalsProvider.Combine(buildSettings), (ctx, pair) =>
        {
            var (data, settings) = pair;

            var options = new ConfiguratorOptions(
                ClassName: $"{data.ImplSimpleName}_GraphLocalConfigurator",
                Namespace: settings.ComputeOutputNamespace("Graphics.RenderGraph"),
                BaseType: "Sparkitect.Graphing.IGraphLocalConfigurator",
                EntrypointAttribute: $"Sparkitect.Graphing.GraphLocalServiceEntryAttribute<{data.GraphBaseFqn}>",
                Kind: new ConfiguratorKind.Service(),
                IsPartial: false,
                ModuleTypeFullName: null);

            var registrations = new[] { data.FactoryData.Registration }.ToImmutableValueArray();

            if (DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName))
                ctx.AddSource(fileName, code);
        });
    }
}

internal record GraphLocalData(
    FactoryWithRegistration FactoryData,
    string InterfaceFqn,
    string GraphBaseFqn,
    string ImplFqn,
    string ImplSimpleName,
    string ImplNamespace);
