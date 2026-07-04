using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.GameState.Diagnostics;

namespace Sparkitect.Generator.GameState.Analyzers;

/// <summary>
/// SPARK0306: local source-generator-input shape guard for the state composition surface.
/// A concrete type carrying a state/module registration attribute must be shaped for the new
/// capability contract — declared <c>partial</c> and either deriving the corresponding transitive
/// base or directly implementing the corresponding capability interface. When it is mis-shaped the
/// registration generator would silently drop it (or emit a cryptic constraint failure); this guard
/// fails loud at compile instead.
///
/// This is a LOCAL type-shape check only. It inspects a single symbol's own attributes, base chain,
/// interfaces, and partial-ness. It never reasons about module presence, closures, or missing/
/// unregistered ids across mod boundaries — that would be a composition analyzer, which is out of scope.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StateModuleShapeAnalyzer : DiagnosticAnalyzer
{
    // Registration signal: the generated nested attributes, applied as
    // [ModuleRegistry.RegisterModule("key")] / [StateRegistry.RegisterState("key")].
    private const string ModuleRegistrationAttribute = "Sparkitect.GameState.ModuleRegistry.RegisterModuleAttribute";
    private const string StateRegistrationAttribute = "Sparkitect.GameState.StateRegistry.RegisterStateAttribute";

    // Capability contracts (direct-implementation escape hatch).
    private const string StateModuleInterface = "Sparkitect.GameState.IStateModule";
    private const string GameStateInterface = "Sparkitect.GameState.IGameState";

    // Heavy-lifting authoring bases (default authoring path).
    private const string TransitiveStateModuleBase = "Sparkitect.GameState.TransitiveStateModule";
    private const string TransitiveGameStateBase = "Sparkitect.GameState.TransitiveGameState";

    private enum RegistrationKind
    {
        None,
        Module,
        State,
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(StateModuleMisshaped);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;

        // Only concrete classes register a state/module; abstract bases and non-class kinds are out.
        if (type.TypeKind != TypeKind.Class) return;
        if (type.IsAbstract) return;

        // Registration signal — inspect ONLY this type's own attributes (local check).
        var kind = GetRegistrationKind(type);
        if (kind == RegistrationKind.None) return;

        var (baseName, interfaceName) = kind == RegistrationKind.Module
            ? (TransitiveStateModuleBase, StateModuleInterface)
            : (TransitiveGameStateBase, GameStateInterface);

        // LOCAL shape checks: this symbol's own base chain, interfaces, and partial-ness only.
        var derivesBase = DerivesFrom(type, baseName);
        var implementsInterface = type.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == interfaceName);
        var isPartial = IsPartial(type);

        var satisfiesContract = (derivesBase || implementsInterface) && isPartial;
        if (satisfiesContract) return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            StateModuleMisshaped,
            type.Locations.FirstOrDefault(),
            type.Name,
            baseName,
            interfaceName));
    }

    private static RegistrationKind GetRegistrationKind(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType);
            if (name == ModuleRegistrationAttribute) return RegistrationKind.Module;
            if (name == StateRegistrationAttribute) return RegistrationKind.State;
        }

        return RegistrationKind.None;
    }

    private static bool DerivesFrom(INamedTypeSymbol type, string baseName)
    {
        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            if (b.ToDisplayString(DisplayFormats.NamespaceAndType) == baseName) return true;
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Any(r =>
            r.GetSyntax() is TypeDeclarationSyntax decl &&
            decl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
    }
}
