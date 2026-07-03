using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HasIdentificationMisuseAnalyzer : DiagnosticAnalyzer
{
    private const string IHasIdentificationDisplayName = "Sparkitect.Modding.IHasIdentification";
    private const string IRegisterMarkerInterface = "Sparkitect.Modding.IRegisterMarker";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RegistryDiagnostics.HasIdentificationMisuse);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Skip SG-emitted source: the auto-emit pipeline emits
        // 'partial T : IHasIdentification' declarations on every type-registered concrete.
        // We must not flag those — they are exactly the canonical shape we want to encourage.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;

        // Only fire when at least one user-source declaration directly lists IHasIdentification
        // in its base list. Walking DeclaringSyntaxReferences keeps the predicate honest:
        // a type that "implements" IHasIdentification only via an SG-emitted partial will
        // not match here (the SG file is filtered out by ConfigureGeneratedCodeAnalysis).
        var hasUserSourceIHasIdDeclaration = false;
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax(ctx.CancellationToken);
            if (syntax is not TypeDeclarationSyntax typeDecl) continue;
            if (typeDecl.BaseList is null) continue;

            // Resolving each base-type symbol from its declaration syntax is required to
            // tell a hand-written ': IHasIdentification' apart from the auto-emitted partial;
            // a symbol action has no cached model, so binding the declaring tree is the only
            // option and the per-type cost is bounded to identified concretes.
#pragma warning disable RS1030 // Compilation.GetSemanticModel is unavoidable in this symbol action
            var semantic = ctx.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
#pragma warning restore RS1030
            foreach (var baseTypeSyntax in typeDecl.BaseList.Types)
            {
                var baseSymbol = semantic.GetSymbolInfo(baseTypeSyntax.Type, ctx.CancellationToken).Symbol;
                if (baseSymbol is INamedTypeSymbol s &&
                    s.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationDisplayName)
                {
                    hasUserSourceIHasIdDeclaration = true;
                    break;
                }
            }

            if (hasUserSourceIHasIdDeclaration) break;
        }

        if (!hasUserSourceIHasIdDeclaration) return;

        // Skip if any attribute on the type is registration-driving (implements IRegisterMarker).
        var hasRegisterMarker = type.GetAttributes()
            .Any(a => a.AttributeClass is { } ac &&
                      ac.AllInterfaces.Any(i =>
                          i.ToDisplayString(DisplayFormats.NamespaceAndType) == IRegisterMarkerInterface));

        if (hasRegisterMarker) return;

        var location = type.Locations.FirstOrDefault();
        ctx.ReportDiagnostic(Diagnostic.Create(
            RegistryDiagnostics.HasIdentificationMisuse,
            location,
            type.Name));
    }
}
