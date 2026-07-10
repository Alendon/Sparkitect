using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Generator;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistryShapeAnalyzer : DiagnosticAnalyzer
{
    private const string TypedIdentificationAttributeName = "Sparkitect.Modding.TypedIdentificationAttribute";
    private const string RegistryAttributeName = "Sparkitect.Modding.RegistryAttribute";

    private static Location? GetAttributeLocation(AttributeData attr)
    {
        return attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        RegistryDiagnostics.RegistryRequiresInterface,
        RegistryDiagnostics.RegistryMissingIdentifier,
        RegistryDiagnostics.RegistryMustBeTopLevelInNamespace,
        RegistryDiagnostics.DuplicateCategoryKey,
        RegistryDiagnostics.DuplicateRegistryTypeNames,
        RegistryDiagnostics.CategoryIdentifierNotSnakeCase,
        RegistryDiagnostics.RegistryMethodOutsideRegistry,
        RegistryDiagnostics.InvalidRegistryMethodSignature,
        RegistryDiagnostics.TooManyTypeParameters,
        RegistryDiagnostics.GenericValueMismatch,
        RegistryDiagnostics.FirstParameterMustBeIdentification,
        RegistryDiagnostics.DuplicateRegistryMethodName,
        RegistryDiagnostics.UseResourceFileMissingKey,
        RegistryDiagnostics.DuplicateResourceFileKey,
        RegistryDiagnostics.MultiplePrimaryResourceFiles,
        RegistryDiagnostics.MultipleBareTypedIdentificationMarkers,
        RegistryDiagnostics.RegistryShapeIncoherent,
        RegistryDiagnostics.InvalidTypedIdentificationTarget,
        RegistryDiagnostics.TypedIdentificationAliasCollision
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeRegistryType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeRegistryMethod, SymbolKind.Method);
    }

    private static void AnalyzeRegistryType(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not INamedTypeSymbol type) return;

        // Look for [Sparkitect.Modding.RegistryAttribute]
        var regAttr = type.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.RegistryAttribute");
        if (regAttr is null) return; // not a registry type

        // Get attribute location for reporting attribute-related errors
        var regAttrLocation = GetAttributeLocation(regAttr);

        // SPARK0201: Must implement the constructed IRegistry<TModule> (the bare non-generic IRegistry
        // no longer satisfies the contract — the owning-module link is carried by the type argument).
        var implementsIRegistry = type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.IRegistry"
            && i.TypeArguments.Length == 1);
        if (!implementsIRegistry)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.RegistryRequiresInterface,
                regAttrLocation ?? type.Locations.FirstOrDefault(),
                type.Name));
        }

        // Extract Identifier value (if present)
        string? identifier = null;
        foreach (var na in regAttr.NamedArguments)
        {
            if (na.Key == "Identifier" && na.Value.Value is string s)
            {
                identifier = s;
                break;
            }
        }

        // SPARK0202: Missing/empty Identifier
        if (string.IsNullOrWhiteSpace(identifier))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.RegistryMissingIdentifier,
                regAttrLocation ?? type.Locations.FirstOrDefault(),
                type.Name));
        }

        // SPARK0203: Must be top-level and in a namespace (not global, not nested)
        var isTopLevel = type.ContainingType is null;
        var hasNamespace = type.ContainingNamespace is { IsGlobalNamespace: false };
        if (!isTopLevel || !hasNamespace)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.RegistryMustBeTopLevelInNamespace,
                regAttrLocation ?? type.Locations.FirstOrDefault(),
                type.Name));
        }

        // SPARK0206: Identifier must be snake_case (letters and underscores only)
        if (!string.IsNullOrWhiteSpace(identifier) && !StringCase.IsSnakeCase(identifier!))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.CategoryIdentifierNotSnakeCase,
                regAttrLocation ?? type.Locations.FirstOrDefault(),
                identifier));
        }

        // SPARK0215: Duplicate registry method names within a registry
        var regMethodNames = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                "Sparkitect.Modding.RegistryMethodAttribute"))
            .GroupBy(m => m.Name);

        foreach (var g in regMethodNames)
        {
            if (g.Count() <= 1) continue;
            // Report on all but the first occurrence for clarity
            foreach (var dup in g.Skip(1))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.DuplicateRegistryMethodName,
                    dup.Locations.FirstOrDefault(),
                    g.Key,
                    type.Name));
            }
        }

        // Analyze UseResourceFile attributes
        var resourceFileAttrs = type.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                        "Sparkitect.Modding.UseResourceFileAttribute"
                        || a.AttributeClass?.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                           "Sparkitect.Modding.UseResourceFileAttribute<TResource>")
            .ToArray();

        var seenKeys = new System.Collections.Generic.HashSet<string>();
        var primaryCount = 0;

        foreach (var attr in resourceFileAttrs)
        {
            var keyArg = attr.NamedArguments.FirstOrDefault(x => x.Key == "Key");
            var key = keyArg.Value.Value as string;

            // Get attribute location for this specific UseResourceFile attribute
            var resourceAttrLocation = GetAttributeLocation(attr);

            // SPARK0216: Key must be non-empty
            if (string.IsNullOrWhiteSpace(key))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.UseResourceFileMissingKey,
                    resourceAttrLocation ?? type.Locations.FirstOrDefault(),
                    type.Name));
                continue;
            }

            // SPARK0217: Duplicate keys
            if (!seenKeys.Add(key!))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.DuplicateResourceFileKey,
                    resourceAttrLocation ?? type.Locations.FirstOrDefault(),
                    key,
                    type.Name));
            }

            // Count primaries
            var primaryArg = attr.NamedArguments.FirstOrDefault(x => x.Key == "Primary");
            if (primaryArg.Value.Value is true)
            {
                primaryCount++;
            }
        }

        // SPARK0218: Multiple primaries
        if (primaryCount > 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.MultiplePrimaryResourceFiles,
                regAttrLocation ?? type.Locations.FirstOrDefault(),
                type.Name));
        }

        // SPARK0272 (D-04): registry-wide typed-identification shape coherence. Every
        // [RegistryMethod]-attributed method on this type must agree on whether it carries a bare
        // [TypedIdentification] marker (None vs Identification<TResult>) — a single type with mixed
        // shapes (some marked, some not) is the exact violation DummyRegistry's
        // RegisterTypedProvider/RegisterValue/RegisterProvider trio demonstrates (D-12).
        var registryMethods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                "Sparkitect.Modding.RegistryMethodAttribute"))
            .ToArray();

        if (registryMethods.Length > 1)
        {
            var shapes = registryMethods.Select(HasBareTypedIdentificationMarker).Distinct().ToArray();
            if (shapes.Length > 1)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.RegistryShapeIncoherent,
                    regAttrLocation ?? type.Locations.FirstOrDefault(),
                    type.Name));
            }
        }
    }

    /// <summary>
    /// Whether any of a register method's type parameters carries the BARE (non-generic)
    /// <c>[TypedIdentification]</c> marker — the same-registry shape opt-in (D-04). A closed
    /// <c>[TypedIdentification&lt;TTarget&gt;]</c> hit does not count; that is a cross-registry linkage
    /// (D-05), analytically separate from this registry's own result shape.
    /// </summary>
    private static bool HasBareTypedIdentificationMarker(IMethodSymbol method) =>
        method.TypeParameters.Any(tp => tp.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
            TypedIdentificationAttributeName && !a.AttributeClass.IsGenericType));

    private static void AnalyzeRegistryMethod(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not IMethodSymbol method) return;

        var methodAttr = method.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
            "Sparkitect.Modding.RegistryMethodAttribute");
        if (methodAttr is null) return;

        // Get attribute location for reporting attribute-related errors
        var attrLocation = GetAttributeLocation(methodAttr);

        // SPARK0210: Must be inside a type implementing IRegistry
        var containing = method.ContainingType;
        var isInsideRegistry = containing?.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.IRegistry") == true;
        if (!isInsideRegistry)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.RegistryMethodOutsideRegistry,
                attrLocation ?? method.Locations.FirstOrDefault()));
            return; // other checks rely on being inside a registry
        }

        // SPARK0271/SPARK0273/SPARK0274: typed-identification marker validation (D-05/D-08). Runs
        // independently of the signature-shape checks below — a method's marker usage can be wrong
        // regardless of whether its parameter shape is also wrong.
        AnalyzeTypedIdentificationMarkers(ctx, method, containing!);

        // Gather basic signature data
        var paramCount = method.Parameters.Length;
        var typeParamCount = method.TypeParameters.Length;

        // 0..N type parameters are accepted (D-02): the arity cap is lifted here in tandem with the
        // generator's collapsed taxonomy (Plan 02), so SPARK0212/TooManyTypeParameters is no longer
        // reported for typeParamCount > 1. The descriptor stays declared/registered in
        // SupportedDiagnostics (it may still be referenced) — only the early report path is removed.

        // Must have 1 or 2 parameters; first must be Identification
        if (paramCount == 0 || paramCount > 2)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.InvalidRegistryMethodSignature,
                attrLocation ?? method.Locations.FirstOrDefault(),
                method.Name));
            return;
        }

        var p0 = method.Parameters[0];
        var p0Type = p0.Type.ToDisplayString(DisplayFormats.NamespaceAndType);
        if (p0Type != "Sparkitect.Modding.Identification")
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.FirstParameterMustBeIdentification,
                attrLocation ?? method.Locations.FirstOrDefault(),
                method.Name));
            return;
        }

        // If two params and one or more type params, the value parameter must reference at least
        // one of the method's type parameters: either the bare `T_i` or a constructed generic
        // mentioning it among its type arguments (e.g. SettingDefinition<T>). The wrapper-over-T
        // shape carries a closed generic value type through registration and is a valid
        // register-method shape — do not flag it as a mismatch. Loop all type parameters (not just
        // index 0) so multi-type-parameter value-source methods are validated correctly (D-02).
        if (paramCount == 2)
        {
            var p1 = method.Parameters[1];
            if (typeParamCount >= 1)
            {
                var referencesAnyTypeParameter = method.TypeParameters.Any(tp =>
                    SymbolEqualityComparer.Default.Equals(p1.Type, tp) ||
                    (p1.Type is INamedTypeSymbol { IsGenericType: true } wrapper &&
                     wrapper.TypeArguments.Any(arg => SymbolEqualityComparer.Default.Equals(arg, tp))));
                if (!referencesAnyTypeParameter)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        RegistryDiagnostics.GenericValueMismatch,
                        attrLocation ?? method.Locations.FirstOrDefault(),
                        method.Name));
                    return;
                }
            }
        }

        // SPARK0215: Duplicate registry method names within a registry
        // Check once per containing type through AnalyzeRegistryType to avoid N^2; noop here.
    }

    /// <summary>
    /// Validates a register method's <c>[TypedIdentification]</c>-family markers (D-05/D-08):
    /// SPARK0271 (at most one bare marker per method), SPARK0273 ([TypedIdentification&lt;TTarget&gt;]
    /// must name a [Registry]-attributed type), and SPARK0274 (same-compilation alias-collision —
    /// best-effort, cannot see across assembly boundaries per RESEARCH Pitfall 3).
    /// </summary>
    private static void AnalyzeTypedIdentificationMarkers(SymbolAnalysisContext ctx, IMethodSymbol method,
        INamedTypeSymbol containingRegistry)
    {
        var bareMarkerCount = 0;
        var crossMarkers = new List<(ITypeParameterSymbol TypeParameter, AttributeData Attribute, INamedTypeSymbol? Target)>();

        foreach (var typeParameter in method.TypeParameters)
        {
            foreach (var attribute in typeParameter.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) != TypedIdentificationAttributeName)
                    continue;

                if (attributeClass.IsGenericType && attributeClass.TypeArguments.Length == 1)
                {
                    crossMarkers.Add((typeParameter, attribute, attributeClass.TypeArguments[0] as INamedTypeSymbol));
                    continue;
                }

                bareMarkerCount++;
                if (bareMarkerCount > 1)
                {
                    // SPARK0271: report every marker beyond the first — at-most-one bare marker (D-08).
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        RegistryDiagnostics.MultipleBareTypedIdentificationMarkers,
                        GetAttributeLocation(attribute) ?? typeParameter.Locations.FirstOrDefault(),
                        method.Name));
                }
            }
        }

        if (crossMarkers.Count == 0) return;

        // Registry-level alias suffix (D-06), for the SPARK0274 candidate-name computation below.
        var regAttr = containingRegistry.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryAttributeName);
        string? aliasSuffix = null;
        if (regAttr is not null)
        {
            foreach (var na in regAttr.NamedArguments)
            {
                if (na.Key == "AliasSuffix" && na.Value.Value is string s)
                {
                    aliasSuffix = s;
                    break;
                }
            }
        }

        var modIdAvailable = ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions
            .TryGetValue("build_property.ModId", out var modId) && !string.IsNullOrWhiteSpace(modId);

        foreach (var (typeParameter, attribute, target) in crossMarkers)
        {
            var location = GetAttributeLocation(attribute) ?? typeParameter.Locations.FirstOrDefault();

            // SPARK0273: TTargetRegistry must resolve to a [Registry]-attributed type (D-05).
            var targetRegAttr = target?.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == RegistryAttributeName);
            if (targetRegAttr is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.InvalidTypedIdentificationTarget,
                    location,
                    target?.Name ?? "?",
                    method.Name));
                continue; // an invalid target has no id space to check for a collision against
            }

            // SPARK0274: same-compilation alias-collision detection (D-08/D-06). Best-effort: computes
            // the candidate alias container ({ModIdPascal}{TargetCategoryPascal}IDs, per D-03) and
            // candidate alias name ({TypeParameterName}{AliasSuffix}) — the closest proxy available at
            // the declaration level, since per-entry property names are only known once registration
            // attributes are walked (Plan 04's emission territory, not this analyzer's). Skipped
            // entirely when the mod id isn't available (nothing to compute the container name from).
            if (!modIdAvailable) continue;

            string? targetIdentifier = null;
            foreach (var na in targetRegAttr.NamedArguments)
            {
                if (na.Key == "Identifier" && na.Value.Value is string s)
                {
                    targetIdentifier = s;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(targetIdentifier)) continue;

            var containerName = $"{StringCase.ToPascalCase(modId!)}{StringCase.ToPascalCase(targetIdentifier!)}IDs";
            var candidateAliasName = $"{typeParameter.Name}{aliasSuffix}";

            foreach (var candidateType in ctx.Compilation.GetSymbolsWithName(containerName, SymbolFilter.Type)
                         .OfType<INamedTypeSymbol>())
            {
                var realMember = candidateType.GetMembers(candidateAliasName)
                    .FirstOrDefault(m => !m.IsImplicitlyDeclared);
                if (realMember is null) continue;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    RegistryDiagnostics.TypedIdentificationAliasCollision,
                    location,
                    candidateAliasName,
                    candidateType.Name));
            }
        }
    }
}
