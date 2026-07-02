using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding.Analyzers;

/// <summary>
/// SPARK0263: flags a registered concrete that is missing an explicit <c>: IHasIdentification</c>
/// declaration in user source. The Registry Generator's auto-emit supplies only the static
/// <c>Identification</c> member; the interface itself must be user-source-visible for the
/// registration constraint <c>where T : ..., IHasIdentification</c> to bind. Reporting here
/// replaces the cryptic CS0311 the generated registration would otherwise produce.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingIdentificationAnalyzer : DiagnosticAnalyzer
{
    private const string IHasIdentificationDisplayName = "Sparkitect.Modding.IHasIdentification";
    private const string IRegisterMarkerInterface = "Sparkitect.Modding.IRegisterMarker";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RegistryDiagnostics.MissingExplicitIdentification);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;

        // Only concrete (non-abstract) class/struct registrations can carry the interface.
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct)) return;
        if (type.IsAbstract) return;

        // Registration signal: a registration-driving attribute (implements IRegisterMarker).
        var isRegistered = type.GetAttributes()
            .Any(a => a.AttributeClass is { } ac &&
                      ac.AllInterfaces.Any(i =>
                          i.ToDisplayString(DisplayFormats.NamespaceAndType) == IRegisterMarkerInterface));

        if (!isRegistered) return;

        // Auto-emit no longer supplies the base-list, so IHasIdentification reaches AllInterfaces
        // only when user source (or a base type) declares it. Its absence is the violation.
        var isIdentified = type.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationDisplayName);

        if (isIdentified) return;

        var location = type.Locations.FirstOrDefault();
        ctx.ReportDiagnostic(Diagnostic.Create(
            RegistryDiagnostics.MissingExplicitIdentification,
            location,
            type.Name));
    }
}
