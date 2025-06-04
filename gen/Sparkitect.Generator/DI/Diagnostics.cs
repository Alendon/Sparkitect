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
        new("SPARK1004", "Only one generation marker per type",
            "Type '{0}' has multiple factory attributes", "Sparkitect", DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor ConflictingGenerationMarker =
        new("SPARK1005", "Conflicting generation markers",
            "Type '{0}' has conflicting factory attributes: {1}", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryRequiresKey =
        new("SPARK1006", "KeyedFactory requires key specification",
            "KeyedFactory '{0}' must specify exactly one key using either [Key] or [KeyProperty] attribute", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryKeyMustBeString =
        new("SPARK1007", "KeyedFactory key must be string type",
            "KeyedFactory key parameter must be of type string, not '{0}'", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor KeyedFactoryInvalidKeyProperty =
        new("SPARK1008", "Invalid KeyProperty reference",
            "KeyProperty '{0}' must reference a valid static property on type '{1}'", "Sparkitect", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor ConflictingKeyAttributes =
        new("SPARK1009", "Multiple key attributes found",
            "KeyedFactory cannot have both [Key] and [KeyProperty] attributes", "Sparkitect", DiagnosticSeverity.Error, true);
}