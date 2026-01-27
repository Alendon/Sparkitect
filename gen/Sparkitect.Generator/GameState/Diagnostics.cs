using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.GameState;

// Category 03: GameState/StateService Diagnostics (SPARK03XX)
// - StateService interface validation
// - Facade implementation validation

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor StateServiceInterfaceNotImplemented =
        new("SPARK0301", "StateService does not implement declared interface",
            "Class '{0}' has [StateService<{1}>] but doesn't implement '{1}'. Add the interface to the class declaration.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateServiceFacadeNotImplemented =
        new("SPARK0302", "StateService does not implement required facade",
            "Class '{0}' has [StateService<{1}>] but doesn't implement required facade '{2}'. Add '{2}' to the class declaration.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateServiceInterfaceMissingFacade =
        new("SPARK0303", "StateService interface missing StateFacade attribute",
            "Interface '{0}' used in [StateService<{0}>] has no [StateFacade<T>] attribute. Add at least one facade attribute to the interface.",
            "Sparkitect", DiagnosticSeverity.Error, true);
}
