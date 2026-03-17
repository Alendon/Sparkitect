using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;

namespace Sparkitect.Generator.GameState;

[Generator]
public class StateModuleServiceGenerator : IIncrementalGenerator
{
    private const string StateServiceAttributeMetadataName = "Sparkitect.GameState.StateServiceAttribute`2";
    private const string StateServiceName = "Sparkitect.GameState.StateServiceAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var stateServicesProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            StateServiceAttributeMetadataName,
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol classSymbol)
                    return null;

                // Extract ALL symbol data at the pipeline boundary
                var stateServiceAttr = classSymbol.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == StateServiceName);
                if (stateServiceAttr?.AttributeClass?.TypeArguments.Length != 2)
                    return null;

                var baseType = stateServiceAttr.AttributeClass.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var moduleType = stateServiceAttr.AttributeClass.TypeArguments[1];

                // Use DiPipeline for factory extraction
                var factory = DiPipeline.ExtractFactory(classSymbol, new FactoryIntent.Service(), baseType);
                if (factory is null) return null;

                var registration = DiPipeline.ToRegistration(factory, classSymbol);

                return new StateServiceData(
                    new FactoryWithRegistration(factory, registration),
                    moduleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    moduleType.Name);
            }).NotNull();

        // Output individual factory classes
        context.RegisterSourceOutput(stateServicesProvider, (ctx, data) =>
        {
            if (DiPipeline.RenderFactory(data.FactoryData.Factory, out var code, out var fileName))
                ctx.AddSource(fileName, code);
        });

        // Group by module and output configurators
        var grouped = stateServicesProvider.Collect();
        context.RegisterSourceOutput(grouped, (ctx, allServices) =>
        {
            var moduleGroups = allServices
                .GroupBy(s => s.ModuleTypeFullName)
                .ToArray();

            foreach (var group in moduleGroups)
            {
                var registrations = group
                    .Select(s => s.FactoryData.Registration)
                    .ToImmutableValueArray();

                var first = group.First();
                var options = new ConfiguratorOptions(
                    ClassName: $"{first.ModuleTypeName}_ServiceConfigurator",
                    Namespace: "Sparkitect.CompilerGenerated.GameState",
                    BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
                    EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
                    Kind: new ConfiguratorKind.Service(),
                    IsPartial: false,
                    ModuleTypeFullName: first.ModuleTypeFullName);

                if (DiPipeline.RenderConfigurator(registrations, options,
                    out var code, out var fileName))
                    ctx.AddSource(fileName, code);
            }
        });
    }
}

internal record StateServiceData(
    FactoryWithRegistration FactoryData,
    string ModuleTypeFullName,
    string ModuleTypeName);
