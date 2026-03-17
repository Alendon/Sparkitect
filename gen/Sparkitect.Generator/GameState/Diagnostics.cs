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

    public static readonly DiagnosticDescriptor FacadeMissingFacadeForAttribute =
        new("SPARK0304", "Facade interface missing [FacadeFor] attribute",
            "Interface '{0}' is referenced by [{1}<{0}>] on '{2}' but does not have a [FacadeFor<{2}>] attribute.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FacadeForInconsistentWithService =
        new("SPARK0305", "FacadeFor attribute inconsistent with service facade declaration",
            "Interface '{0}' has [FacadeFor<{1}>] but '{1}' does not have a facade marker attribute referencing '{0}'.",
            "Sparkitect", DiagnosticSeverity.Error, true);
}
