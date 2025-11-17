using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.GameState.Diagnostics;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.GameState.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateServiceAnalyzer : DiagnosticAnalyzer
{
    private const string StateServiceAttributeMetadataName = "Sparkitect.GameState.StateServiceAttribute`1";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        StateServiceInterfaceNotImplemented,
        StateServiceFacadeNotImplemented,
        StateServiceInterfaceMissingFacade);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateStateService, SymbolKind.NamedType);
    }

    private void ValidateStateService(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
            return;

        // Find all StateService attributes on this type
        var stateServiceAttributes = type.GetAttributes()
            .Where(attr => attr.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) == StateServiceAttributeMetadataName)
            .ToList();

        if (!stateServiceAttributes.Any())
            return;

        foreach (var attribute in stateServiceAttributes)
        {
            ValidateSingleStateService(context, type, attribute);
        }
    }

    private void ValidateSingleStateService(SymbolAnalysisContext context, INamedTypeSymbol implementationType, AttributeData attribute)
    {
        // Extract the interface type from StateService<TInterface>
        if (attribute.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol interfaceType)
            return;

        // Validate: Implementation must implement the declared interface
        if (!ImplementsInterface(implementationType, interfaceType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateServiceInterfaceNotImplemented,
                implementationType.Locations.FirstOrDefault(),
                implementationType.Name,
                interfaceType.Name));
            return; // No point checking facades if interface isn't implemented
        }

        // Get all StateFacade attributes from the interface
        var facadeAttributes = interfaceType.GetAttributes()
            .Where(attr => IsStateFacadeAttribute(attr))
            .ToList();

        // Validate: Interface must have at least one StateFacade attribute
        if (!facadeAttributes.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateServiceInterfaceMissingFacade,
                implementationType.Locations.FirstOrDefault(),
                interfaceType.Name));
            return;
        }

        // Validate: Implementation must implement all required facades
        foreach (var facadeAttr in facadeAttributes)
        {
            if (facadeAttr.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol facadeType)
                continue;

            if (!ImplementsInterface(implementationType, facadeType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateServiceFacadeNotImplemented,
                    implementationType.Locations.FirstOrDefault(),
                    implementationType.Name,
                    interfaceType.Name,
                    facadeType.Name));
            }
        }
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceType)
    {
        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
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
