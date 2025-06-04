using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.DI;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor OnlyAbstractDependencies =
        new("SPARK1001", "Use only abstract/interface dependencies",
            "Dependency '{0}' of type '{1}' should be an interface or abstract class", "Sparkitect", DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor OnlyOneConstructor =
        new("SPARK1002", "Only one constructor allowed",
            "Type '{0}' must have exactly one constructor when marked with factory attribute", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor RequiredPropertiesInitOnly =
        new("SPARK1003", "Required properties should be init-only", 
            "Required property '{0}' should have an init-only setter", "Sparkitect", DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor SingleGenerationMarker =
        new("SPARK1004", "Exactly one generation marker per type",
            "Type '{0}' has multiple factory markers", "Sparkitect", DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor ConflictingGenerationMarker =
        new("SPARK1005", "Conflicting generation markers",
            "Type '{0}' has conflicting factory attributes: {1}", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryMissingKey =
        new("SPARK1006", "KeyedFactory missing key association",
            "KeyedFactory '{0}' must have exactly one key association (either Key or KeyPropertyName)", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryInvalidKeyProperty =
        new("SPARK1007", "Invalid KeyProperty reference",
            "KeyProperty '{0}' on type '{1}' must reference a valid static property with a public getter that returns string, Identification, or OneOf<Identification, string>", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryConflictingKeys =
        new("SPARK1008", "Conflicting key associations",
            "KeyedFactory '{0}' cannot have both Key and KeyPropertyName set simultaneously", "Sparkitect", DiagnosticSeverity.Error, true);
}