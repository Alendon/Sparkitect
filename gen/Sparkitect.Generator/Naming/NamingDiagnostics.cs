using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Naming;

// Category 05: Naming Diagnostics (SPARK05XX)
// - ModId validation (01-09)
// - Parameter attribute validation (10-19)

/// <summary>
/// Diagnostic descriptors for naming validation.
/// </summary>
public static class NamingDiagnostics
{
    private const string Category = "Sparkitect";

    // SPARK0501: ModId in csproj must be snake_case
    public static readonly DiagnosticDescriptor ModIdNotSnakeCase =
        new("SPARK0501", "ModId must be snake_case",
            "Invalid identifier '{0}' - must be snake_case",
            Category, DiagnosticSeverity.Error, true,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

    // SPARK0502: String argument to [SnakeCase] parameter must be snake_case
    public static readonly DiagnosticDescriptor IdentifierNotSnakeCase =
        new("SPARK0502", "Identifier must be snake_case",
            "Invalid identifier '{0}' - must be snake_case",
            Category, DiagnosticSeverity.Error, true);
}
