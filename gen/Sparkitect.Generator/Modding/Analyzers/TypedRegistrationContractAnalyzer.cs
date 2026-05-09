using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding.Analyzers;

/// <summary>
/// SPARK0263 (D-16 follow-on, Phase 49.3-04): promotes use of [TypedRegistrationContract] on the
/// base type referenced by a typed-registration registry method's generic constraint.
///
/// <para>A typed-registration registry method has the shape
/// <c>[RegistryMethod] void RegisterX&lt;T&gt;(Identification id) where T : class, TBase, IHasIdentification</c>
/// — TBase is the contract surface that final concretes derive from. After Phase 49.3-04 dropped
/// <c>: IHasIdentification</c> from contract interfaces (D-09), final concretes acquire
/// IHasIdentification only through <c>RegistryGenerator</c> auto-emit. Sibling generators
/// (e.g. <c>StatelessFunctionGenerator</c>) cannot observe that auto-emit output within the same
/// compilation pass, so they must instead look for <c>[TypedRegistrationContract]</c> on TBase.</para>
///
/// <para>This analyzer surfaces the missing attribute as a warning so authors of typed-registration
/// registries are nudged toward the new contract attribute when their TBase is user-source and not
/// itself <c>IHasIdentification</c>.</para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedRegistrationContractAnalyzer : DiagnosticAnalyzer
{
    private const string RegistryMethodAttributeDisplayName =
        "Sparkitect.Modding.RegistryMethodAttribute";

    private const string IdentificationDisplayName =
        "Sparkitect.Modding.Identification";

    private const string IHasIdentificationDisplayName =
        "Sparkitect.Modding.IHasIdentification";

    private const string TypedRegistrationContractAttributeDisplayName =
        "Sparkitect.Modding.TypedRegistrationContractAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RegistryDiagnostics.TypedRegistrationContractMissing);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        var method = (IMethodSymbol)ctx.Symbol;

        // Only fires on [RegistryMethod] methods.
        var registryMethodAttr = method.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                is RegistryMethodAttributeDisplayName);

        if (registryMethodAttr is null) return;

        // Typed-registration shape: exactly one type parameter, exactly one Identification parameter.
        if (method.TypeParameters.Length != 1) return;
        if (method.Parameters.Length != 1) return;
        if (method.Parameters[0].Type.ToDisplayString(DisplayFormats.NamespaceAndType)
                is not IdentificationDisplayName) return;

        var typeParam = method.TypeParameters[0];

        // Require the IHasIdentification constraint as a marker that this is the
        // typed-registration shape (Phase 49.2 SPARK0261 already mandates it).
        var hasIHasIdentificationConstraint = typeParam.ConstraintTypes.Any(t =>
            t.ToDisplayString(DisplayFormats.NamespaceAndType)
                is IHasIdentificationDisplayName);

        if (!hasIHasIdentificationConstraint) return;

        // Each non-IHasIdentification constraint type is a candidate "TBase". Report on every
        // candidate that is itself NOT IHasIdentification and lacks [TypedRegistrationContract].
        // This naturally covers the common shape `where T : class, TBase, IHasIdentification`
        // without us needing to second-guess which constraint is the "real" TBase.
        foreach (var constraintType in typeParam.ConstraintTypes)
        {
            if (constraintType is not INamedTypeSymbol tBase) continue;

            // Skip IHasIdentification itself — it's the canonical positive-case constraint.
            if (tBase.ToDisplayString(DisplayFormats.NamespaceAndType)
                    is IHasIdentificationDisplayName)
            {
                continue;
            }

            // Skip types that already implement IHasIdentification on the contract side
            // (e.g. an abstract base that pre-49.3 still legitimately listed it). We want to
            // promote [TypedRegistrationContract] on the contracts where IHasIdentification is
            // absent, which is the post-49.3 canonical shape.
            var tBaseImplementsIHasIdentification = tBase.AllInterfaces.Any(i =>
                i.ToDisplayString(DisplayFormats.NamespaceAndType) is IHasIdentificationDisplayName);

            if (tBaseImplementsIHasIdentification) continue;

            // Skip if the contract type is metadata-only (declared in another assembly we don't
            // own) — we have no authority to suggest changes there. This filters out cases like
            // a registry whose TBase is `object` or some BCL type. Since we already filtered to
            // `INamedTypeSymbol` and excluded IHasIdentification, the remaining check is whether
            // the type has at least one declaring syntax reference (i.e., is in source).
            if (tBase.DeclaringSyntaxReferences.Length == 0) continue;

            // Already carries [TypedRegistrationContract]? Walk the contract chain — the
            // attribute is Inherited=true so derived contract types receive it transparently.
            if (HasTypedRegistrationContractAttribute(tBase)) continue;

            // Report at the registry-method declaration site so the author sees the suggestion
            // alongside the [RegistryMethod] application.
            var methodLocation = method.Locations.FirstOrDefault();
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.TypedRegistrationContractMissing,
                methodLocation,
                tBase.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                method.Name));
        }
    }

    private static bool HasTypedRegistrationContractAttribute(INamedTypeSymbol type)
    {
        // Direct attributes on the type. (Inherited=true means derived interfaces/classes also
        // receive the attribute via the language's attribute-inheritance rules at use sites,
        // but here we're checking the contract type itself; if a base contract carries it,
        // the inheritance check below picks it up.)
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                    is TypedRegistrationContractAttributeDisplayName)
            {
                return true;
            }
        }

        // Walk implemented interfaces (transitive closure).
        foreach (var iface in type.AllInterfaces)
        {
            foreach (var attr in iface.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                        is TypedRegistrationContractAttributeDisplayName)
                {
                    return true;
                }
            }
        }

        // Walk base classes.
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            foreach (var attr in baseType.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                        is TypedRegistrationContractAttributeDisplayName)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
