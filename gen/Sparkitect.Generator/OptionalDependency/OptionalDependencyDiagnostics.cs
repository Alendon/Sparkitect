using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.OptionalDependency;

// Category 06: Optional Dependency Diagnostics (SPARK06XX)
// - Type leakage detection (01-09)
// - Guard enforcement (10-19)
// - Attribute validation (20-29)

/// <summary>
/// Diagnostic descriptors for optional dependency validation.
/// </summary>
public static class OptionalDependencyDiagnostics
{
    private const string Category = "Sparkitect";

    // SPARK0601: Type from optional mod used outside guarded context
    public static readonly DiagnosticDescriptor TypeLeakage =
        new("SPARK0601", "Optional mod type leakage",
            "Type '{0}' from optional mod '{1}' used outside guarded context. " +
            "Wrap access in a class marked [OptionalModDependent(\"{1}\")] or " +
            "method marked [ModLoadedGuard(\"{1}\")].",
            Category, DiagnosticSeverity.Error, true,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

    // SPARK0602: Call to [OptionalModDependent] code without going through [ModLoadedGuard]
    public static readonly DiagnosticDescriptor UnguardedCall =
        new("SPARK0602", "Unguarded optional mod call",
            "Call to '{0}' requires going through a [ModLoadedGuard(\"{1}\")] method",
            Category, DiagnosticSeverity.Error, true,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

    // SPARK0603: ModId in attribute doesn't match any declared optional dependency
    public static readonly DiagnosticDescriptor InvalidModId =
        new("SPARK0603", "Invalid optional mod ID",
            "Mod ID '{0}' in [{1}] is not declared as an optional dependency. " +
            "Add <ModProjectDependency Include=\"...\" IsOptional=\"true\" /> to the project.",
            Category, DiagnosticSeverity.Error, true,
            customTags: WellKnownDiagnosticTags.CompilationEnd);
}
