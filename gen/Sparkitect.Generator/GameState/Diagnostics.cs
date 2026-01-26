using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.GameState;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor StateServiceInterfaceNotImplemented =
        new("SPARK3012", "StateService does not implement declared interface",
            "Class '{0}' marked with [StateService<{1}>] does not implement interface '{1}'",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateServiceFacadeNotImplemented =
        new("SPARK3013", "StateService does not implement required facade",
            "Class '{0}' marked with [StateService<{1}>] does not implement required facade '{2}' from interface '{1}'",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateServiceInterfaceMissingFacade =
        new("SPARK3014", "StateService interface missing StateFacade attribute",
            "Interface '{0}' used in [StateService<{0}>] must have at least one [StateFacade<T>] attribute",
            "Sparkitect", DiagnosticSeverity.Error, true);
}