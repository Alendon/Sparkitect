using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Stateless.Analyzers;

// Category 04: Stateless Function Diagnostics (SPARK04XX)
// - Static method requirement (01)
// - Single scheduling attribute (02)
// - DI-resolvable parameters (03)
// - IHasIdentification container (04)
// - Orphan ordering attributes (05)

public static class StatelessDiagnostics
{
    private const string Category = "Sparkitect";

    public static readonly DiagnosticDescriptor MethodMustBeStatic =
        new("SPARK0401", "Stateless function must be static",
            "Method '{0}' has [StatelessFunctionAttribute] but is not static. Add the 'static' modifier.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MultipleSchedulingAttributes =
        new("SPARK0402", "Multiple scheduling attributes not allowed",
            "Method '{0}' has multiple scheduling attributes. Use exactly one scheduling attribute per stateless function.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ParameterNotDiResolvable =
        new("SPARK0403", "Parameter may not be DI-resolvable",
            "Parameter '{0}' of type '{1}' may not be DI-resolvable. Use an interface/abstract class or make nullable.",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MissingIHasIdentification =
        new("SPARK0404", "Container must implement IHasIdentification",
            "Method '{0}' is in type '{1}' which does not implement IHasIdentification. Add IHasIdentification to the type or use [ParentId<T>].",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor OrphanOrderingAttribute =
        new("SPARK0405", "Ordering attribute without scheduling",
            "Method '{0}' has {1} but no scheduling attribute. Add a scheduling attribute or remove the ordering attribute.",
            Category, DiagnosticSeverity.Warning, true);
}
