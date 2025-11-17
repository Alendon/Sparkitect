using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.GameState;

[Generator]
public class StateServiceMappingGenerator : IIncrementalGenerator
{
    private const string StateServiceAttributeMetadataName = "Sparkitect.GameState.StateServiceAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes marked with [StateService<T>]
        var stateServiceProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            StateServiceAttributeMetadataName,
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol classSymbol)
                    return null;

                return ExtractServiceFacadeMapping(classSymbol);
            }).Where(m => m is not null)!;

        // Collect all mappings and generate single entrypoint
        var allMappingsProvider = stateServiceProvider.Collect();

        context.RegisterSourceOutput(allMappingsProvider, (context, mappings) =>
        {
            if (mappings.IsEmpty)
                return;

            if (RenderStateServiceMapping(mappings, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });
    }

    internal static ServiceFacadeMapping? ExtractServiceFacadeMapping(INamedTypeSymbol serviceType)
    {
        // Find StateService attribute
        var stateServiceAttr = serviceType.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) == StateServiceAttributeMetadataName);

        if (stateServiceAttr is null)
            return null;

        // Get the interface type from StateService<TInterface>
        if (stateServiceAttr.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol interfaceType)
            return null;

        // Extract facade types from interface's StateFacade attributes
        var facadeTypes = new List<string>();
        foreach (var attr in interfaceType.GetAttributes())
        {
            if (!IsStateFacadeAttribute(attr))
                continue;

            if (attr.AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol facadeType)
            {
                facadeTypes.Add(facadeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        // Only include if there are facade types (analyzer will catch missing facades)
        if (facadeTypes.Count == 0)
            return null;

        return new ServiceFacadeMapping(
            interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            facadeTypes.OrderBy(f => f).ToImmutableValueArray());
    }

    internal static bool RenderStateServiceMapping(ImmutableArray<ServiceFacadeMapping> mappings, out string code, out string fileName)
    {
        fileName = "StateServiceMapping.g.cs";

        // Sort mappings for determinism
        var sortedMappings = mappings.OrderBy(m => m.InterfaceType).ToArray();

        // Determine output namespace (use first mapping's namespace or fallback)
        var firstNamespace = sortedMappings.FirstOrDefault()?.InterfaceType
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        var outputNamespace = firstNamespace ?? "Sparkitect.CompilerGenerated";

        var model = new StateServiceMappingModel(
            outputNamespace,
            "GeneratedStateServiceMapping",
            sortedMappings.ToImmutableValueArray());

        return FluidHelper.TryRenderTemplate("GameState.StateServiceMapping.liquid", model, out code);
    }

    private static bool IsStateFacadeAttribute(AttributeData attribute)
    {
        if (attribute.AttributeClass is null)
            return false;

        // Check if the attribute inherits from FacadeMarkerAttribute<T>
        var currentType = attribute.AttributeClass;
        while (currentType != null)
        {
            if (currentType.ConstructedFrom?.ToDisplayString(DisplayFormats.NamespaceAndType) == FacadeMarkerBase)
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }
}
