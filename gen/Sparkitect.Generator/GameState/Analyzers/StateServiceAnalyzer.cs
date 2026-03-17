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
    private const string StateServiceAttributeMetadataName = "Sparkitect.GameState.StateServiceAttribute";
    private const string CoreModuleTypeName = "Sparkitect.GameState.CoreModule";
    private const string FacadeForBaseName = "Sparkitect.DI.GeneratorAttributes.FacadeForAttribute";

    private static Location? GetAttributeLocation(AttributeData attr)
    {
        return attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        StateServiceInterfaceNotImplemented,
        StateServiceFacadeNotImplemented,
        FacadeMissingFacadeForAttribute,
        FacadeForInconsistentWithService);

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
        // Extract the interface type from StateService<TInterface, TModule>
        if (attribute.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol interfaceType)
            return;

        // Extract the module type (second type argument)
        var moduleType = attribute.AttributeClass?.TypeArguments.Length >= 2
            ? attribute.AttributeClass.TypeArguments[1]
            : null;

        // Get attribute location for reporting attribute-related errors
        var attrLocation = GetAttributeLocation(attribute);

        // Validate: Implementation must implement the declared interface
        if (!ImplementsInterface(implementationType, interfaceType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateServiceInterfaceNotImplemented,
                attrLocation ?? implementationType.Locations.FirstOrDefault(),
                implementationType.Name,
                interfaceType.Name));
            return; // No point checking facades if interface isn't implemented
        }

        // Skip facade validation for CoreModule services — they are global engine services
        // that don't participate in the module-scoped facade pattern
        if (moduleType?.ToDisplayString(DisplayFormats.NamespaceAndType) == CoreModuleTypeName)
            return;

        // Get all facade marker attributes from the interface (StateFacade, RegistryFacade, etc.)
        var facadeAttributes = interfaceType.GetAttributes()
            .Where(attr => IsStateFacadeAttribute(attr))
            .ToList();

        // If no facades, that's fine (SPARK0303 removed -- facades are optional)
        // But if facades exist, validate implementation and consistency

        // Validate: Implementation must implement all required facades (SPARK0302)
        // and facade has [FacadeFor<interfaceType>] back-tracking (SPARK0304)
        foreach (var facadeAttr in facadeAttributes)
        {
            if (facadeAttr.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol facadeType)
                continue;

            if (!ImplementsInterface(implementationType, facadeType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateServiceFacadeNotImplemented,
                    attrLocation ?? implementationType.Locations.FirstOrDefault(),
                    implementationType.Name,
                    interfaceType.Name,
                    facadeType.Name));
            }

            // SPARK0304: Check that facadeType has [FacadeFor<interfaceType>]
            var hasFacadeFor = HasFacadeForAttribute(facadeType, interfaceType);
            if (!hasFacadeFor)
            {
                // Get the category attribute name for the error message (e.g., "StateFacade")
                var categoryName = facadeAttr.AttributeClass.Name.Replace("Attribute", "");
                context.ReportDiagnostic(Diagnostic.Create(
                    FacadeMissingFacadeForAttribute,
                    facadeType.Locations.FirstOrDefault() ?? attrLocation ?? implementationType.Locations.FirstOrDefault(),
                    facadeType.Name,
                    categoryName,
                    interfaceType.Name));
            }
        }

        // SPARK0305: Check reverse direction -- for each facade referenced by this interface,
        // verify any [FacadeFor<T>] on the facade points back to this interface
        foreach (var facadeAttr in facadeAttributes)
        {
            if (facadeAttr.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol facadeType)
                continue;

            foreach (var facadeForAttr in facadeType.GetAttributes())
            {
                if (facadeForAttr.AttributeClass is null) continue;
                if (facadeForAttr.AttributeClass.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) != FacadeForBaseName)
                    continue;

                if (facadeForAttr.AttributeClass.TypeArguments.FirstOrDefault() is INamedTypeSymbol targetService
                    && !SymbolEqualityComparer.Default.Equals(targetService, interfaceType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FacadeForInconsistentWithService,
                        facadeType.Locations.FirstOrDefault() ?? attrLocation ?? implementationType.Locations.FirstOrDefault(),
                        facadeType.Name,
                        targetService.Name));
                }
            }
        }
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceType)
    {
        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
    }

    private static bool HasFacadeForAttribute(INamedTypeSymbol facadeType, INamedTypeSymbol expectedServiceType)
    {
        foreach (var attr in facadeType.GetAttributes())
        {
            if (attr.AttributeClass is null) continue;
            if (attr.AttributeClass.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) != FacadeForBaseName)
                continue;

            if (attr.AttributeClass.TypeArguments.FirstOrDefault() is INamedTypeSymbol targetService
                && SymbolEqualityComparer.Default.Equals(targetService, expectedServiceType))
            {
                return true;
            }
        }
        return false;
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
