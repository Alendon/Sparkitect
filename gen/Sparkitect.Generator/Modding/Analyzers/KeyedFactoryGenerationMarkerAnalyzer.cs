using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KeyedFactoryGenerationMarkerAnalyzer : DiagnosticAnalyzer
{
    private const string MarkerAttributeDisplayName =
        "Sparkitect.Modding.KeyedFactoryGenerationMarkerAttribute";

    private const string RegistryMethodAttributeDisplayName =
        "Sparkitect.Modding.RegistryMethodAttribute";

    private const string IdentificationDisplayName =
        "Sparkitect.Modding.Identification";

    private const string IHasIdentificationDisplayName =
        "Sparkitect.Modding.IHasIdentification";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            RegistryDiagnostics.KeyedFactoryMarkerInvalidPlacement,
            RegistryDiagnostics.KeyedFactoryMarkerMissingConstraints);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        var method = (IMethodSymbol)ctx.Symbol;

        // Find the KeyedFactoryGenerationMarkerAttribute<TBase> on this method
        AttributeData? markerAttr = null;
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType)
                is MarkerAttributeDisplayName)
            {
                markerAttr = attr;
                break;
            }
        }

        if (markerAttr is null) return;

        // Recover TBase from the generic argument
        if (markerAttr.AttributeClass is not { TypeArguments: { Length: 1 } } attrClass) return;
        var markerTBase = attrClass.TypeArguments[0] as INamedTypeSymbol;
        if (markerTBase is null) return;

        // Get location: prefer the attribute application site
        var markerLocation = markerAttr.ApplicationSyntaxReference?.GetSyntax(ctx.CancellationToken).GetLocation()
                             ?? method.Locations.FirstOrDefault();

        // --- D-10 (SPARK0260): check [RegistryMethod] AND type-registration shape ---
        var hasRegistryMethod = method.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                      is RegistryMethodAttributeDisplayName);

        var isTypeRegistrationShape =
            method.TypeParameters.Length == 1 &&
            method.Parameters.Length == 1 &&
            method.Parameters[0].Type.ToDisplayString(DisplayFormats.NamespaceAndType)
                is IdentificationDisplayName;

        if (!hasRegistryMethod || !isTypeRegistrationShape)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.KeyedFactoryMarkerInvalidPlacement,
                markerLocation,
                method.Name));
            return;
        }

        // --- D-12 (SPARK0261): literal class, TBase, IHasIdentification constraints ---
        var typeParam = method.TypeParameters[0];

        var hasClassConstraint = typeParam.HasReferenceTypeConstraint;
        var hasTBaseConstraint = typeParam.ConstraintTypes
            .Any(t => SymbolEqualityComparer.Default.Equals(t, markerTBase));
        var hasIHasIdentificationConstraint = typeParam.ConstraintTypes
            .Any(t => t.ToDisplayString(DisplayFormats.NamespaceAndType) is IHasIdentificationDisplayName);

        if (!hasClassConstraint || !hasTBaseConstraint || !hasIHasIdentificationConstraint)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.KeyedFactoryMarkerMissingConstraints,
                markerLocation,
                method.Name,
                markerTBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }
    }
}
