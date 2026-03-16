using System.Collections.Generic;
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
        var buildSettings = context.GetModBuildSettings();

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

                // Extract facade metadata at the symbol boundary
                var facadeMetadata = DiPipeline.ExtractFacadeMetadata(classSymbol, "Sparkitect.GameState.StateFacadeAttribute")
                    .ToImmutableValueArray();

                return new StateServiceData(
                    new FactoryWithRegistration(factory, registration),
                    moduleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    moduleType.Name,
                    facadeMetadata);
            }).NotNull();

        // Output individual factory classes and metadata entrypoints
        context.RegisterSourceOutput(stateServicesProvider.Combine(buildSettings), (ctx, pair) =>
        {
            var (data, settings) = pair;

            if (DiPipeline.RenderFactory(data.FactoryData.Factory, out var code, out var fileName))
                ctx.AddSource(fileName, code);

            // Emit metadata entrypoint if facade metadata was extracted
            if (data.FacadeMetadata.Count > 0)
            {
                var factory = data.FactoryData.Factory;
                var wrapperTypeName = $"{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_Factory";

                var models = data.FacadeMetadata.Cast<IMetadataModel>().ToList();
                if (DiPipeline.RenderMetadataEntrypoint(wrapperTypeName, factory.ImplementationNamespace, models, settings,
                        out var metaCode, out var metaFileName))
                    ctx.AddSource(metaFileName, metaCode);
            }
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
    string ModuleTypeName,
    ImmutableValueArray<FacadeMetadataModel> FacadeMetadata);
