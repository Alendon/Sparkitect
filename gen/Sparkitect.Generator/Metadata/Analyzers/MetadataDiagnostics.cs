using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Metadata.Analyzers;

// Category 07: Metadata parameter placement diagnostics (SPARK07XX)
// - Orphan metadata parameter attribute (01)

public static class MetadataDiagnostics
{
    private const string Category = "Sparkitect";

    public static readonly DiagnosticDescriptor OrphanMetadataParameter =
        new("SPARK0701", "Metadata parameter attribute is not harvested here",
            "{0} on '{1}' is harvestable at this scope but no metadata category present on '{1}' harvests it. Add a metadata attribute whose payload harvests it (for example a scheduling attribute on a function, or the system-group scheduling attribute on a group) or remove the parameter attribute.",
            Category, DiagnosticSeverity.Warning, true);
}
