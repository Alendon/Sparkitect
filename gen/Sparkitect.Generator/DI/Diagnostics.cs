using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.DI;

// Category 01: DI/Factory Diagnostics (SPARK01XX)
// - Constructor and property validation
// - Factory attribute validation
// - Keyed factory rules

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor OnlyAbstractDependencies =
        new("SPARK0101", "Use only abstract/interface dependencies",
            "Dependency '{0}' is type '{1}'. Use an interface or abstract class for DI resolution.",
            "Sparkitect", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor OnlyOneConstructor =
        new("SPARK0102", "Only one constructor allowed",
            "Type '{0}' has multiple constructors. Remove extra constructors - DI factories require exactly one.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RequiredPropertiesInitOnly =
        new("SPARK0103", "Required properties should be init-only",
            "Required property '{0}' has a regular setter. Change to init-only setter (init instead of set).",
            "Sparkitect", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor SingleGenerationMarker =
        new("SPARK0104", "Exactly one generation marker per type",
            "Type '{0}' has multiple factory markers. Remove duplicate factory attributes - only one allowed.",
            "Sparkitect", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor ConflictingGenerationMarker =
        new("SPARK0105", "Conflicting generation markers",
            "Type '{0}' has conflicting factory attributes: {1}. Use only one factory type per class.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor KeyedFactoryMissingKey =
        new("SPARK0106", "KeyedFactory missing key association",
            "KeyedFactory '{0}' has no key. Set either Key or KeyPropertyName in the attribute.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor KeyedFactoryInvalidKeyProperty =
        new("SPARK0107", "Invalid KeyProperty reference",
            "KeyProperty '{0}' on '{1}' is invalid. Must be a public static property returning string, Identification, or OneOf<Identification, string>.",
            "Sparkitect", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor KeyedFactoryConflictingKeys =
        new("SPARK0108", "Conflicting key associations",
            "KeyedFactory '{0}' has both Key and KeyPropertyName. Remove one - only one key source allowed.",
            "Sparkitect", DiagnosticSeverity.Error, true);
}
