using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.GameState;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor StateFunctionMissingSchedule =
        new("SPARK3001", "StateFunction missing schedule attribute",
            "Method '{0}' marked with [StateFunction] must have exactly one scheduling attribute ([PerFrame], [OnStateEnter], [OnStateExit], [OnModuleEnter], or [OnModuleExit])",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionMultipleSchedules =
        new("SPARK3002", "StateFunction has multiple schedule attributes",
            "Method '{0}' has multiple scheduling attributes; only one is allowed",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionNotStatic =
        new("SPARK3003", "StateFunction must be static",
            "Method '{0}' marked with [StateFunction] must be static",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionDuplicateKey =
        new("SPARK3004", "Duplicate StateFunction key within module",
            "Module '{0}' has multiple state functions with key '{1}'",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionParameterNotAbstract =
        new("SPARK3005", "StateFunction parameter must be abstract or interface",
            "Parameter '{0}' of type '{1}' in state function should be an interface or abstract class",
            "Sparkitect", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor OrderingTargetNotFound =
        new("SPARK3006", "Ordering target not found",
            "Ordering attribute references key '{0}' which could not be found in the specified context",
            "Sparkitect", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor OrderingCycleDetected =
        new("SPARK3007", "Ordering cycle detected",
            "A cycle was detected in state function ordering constraints involving '{0}'",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor OrderingInvalidTargetType =
        new("SPARK3008", "Invalid ordering target type",
            "Generic ordering attribute on method '{0}' references type '{1}' which is not a valid module or state descriptor",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ModuleOrderingCycleDetected =
        new("SPARK3009", "Module ordering cycle detected",
            "A cycle was detected in module ordering constraints involving '{0}'",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionInvalidKey =
        new("SPARK3010", "StateFunction has invalid key",
            "Method '{0}' has an invalid or empty key in [StateFunction] attribute",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StateFunctionNotInModule =
        new("SPARK3011", "StateFunction not in module",
            "Method '{0}' marked with [StateFunction] must be declared within a type that implements IStateModule",
            "Sparkitect", DiagnosticSeverity.Error, true);

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