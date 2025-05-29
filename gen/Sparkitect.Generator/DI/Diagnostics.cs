using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.DI;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor OnlyAbstractDependencies =
        new("SPARK1001", "Use only abstract/interface dependencies",
            "Use only abstract/interface dependencies", "Sparkitect", DiagnosticSeverity.Warning, true);
    
    public static readonly DiagnosticDescriptor OnlyOneConstructor =
        new("SPARK1002", "Only one constructor allowed",
            "{0} exposes more than a singular constructor", "Sparkitect", DiagnosticSeverity.Error, true);
}