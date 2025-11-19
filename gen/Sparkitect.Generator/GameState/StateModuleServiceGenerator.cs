using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.GameState.StateUtils;

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

                return ExtractStateServiceInfo(classSymbol);
            }).NotNull();

        var groupedServicesProvider = stateServicesProvider
            .Collect()
            .Select((services, _) => GroupServicesByModule(services));

        context.RegisterSourceOutput(groupedServicesProvider, (context, moduleGroups) =>
        {
            foreach (var moduleGroup in moduleGroups)
            {
                if (RenderModuleServiceConfigurator(moduleGroup, out var code, out var fileName))
                {
                    context.AddSource(fileName, code);
                }
            }
        });
    }

    internal static StateServiceInfo? ExtractStateServiceInfo(INamedTypeSymbol classSymbol)
    {
        var stateServiceAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == StateServiceName);

        if (stateServiceAttr?.AttributeClass is null)
            return null;

        if (stateServiceAttr.AttributeClass.TypeArguments.Length != 2)
            return null;

        var moduleType = stateServiceAttr.AttributeClass.TypeArguments[1];
        var factoryTypeName = $"global::{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}_Factory";

        return new StateServiceInfo(
            moduleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            moduleType.Name,
            factoryTypeName);
    }

    internal static IEnumerable<ModuleServiceGroup> GroupServicesByModule(IEnumerable<StateServiceInfo> services)
    {
        return services
            .GroupBy(s => s.ModuleTypeFullName)
            .Select(g => new ModuleServiceGroup(
                g.Key,
                g.First().ModuleTypeName,
                g.Select(s => new StateServiceFactory(s.FactoryTypeName)).ToImmutableValueArray()))
            .ToArray();
    }

    internal static bool RenderModuleServiceConfigurator(ModuleServiceGroup moduleGroup, out string code, out string fileName)
    {
        var className = $"{moduleGroup.ModuleTypeName}_ServiceConfigurator";
        fileName = $"{className}.g.cs";

        var model = new StateModuleServiceConfiguratorModel(
            "Sparkitect.CompilerGenerated.GameState",
            className,
            moduleGroup.ModuleTypeName,
            moduleGroup.ModuleTypeFullName,
            moduleGroup.ServiceFactories);

        return FluidHelper.TryRenderTemplate("GameState.StateModuleServiceConfigurator.liquid", model, out code);
    }
}

internal record StateServiceInfo(
    string ModuleTypeFullName,
    string ModuleTypeName,
    string FactoryTypeName);

internal record ModuleServiceGroup(
    string ModuleTypeFullName,
    string ModuleTypeName,
    ImmutableValueArray<StateServiceFactory> ServiceFactories);
