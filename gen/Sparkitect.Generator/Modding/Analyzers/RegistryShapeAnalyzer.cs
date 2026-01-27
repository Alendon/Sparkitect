using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Generator;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistryShapeAnalyzer : DiagnosticAnalyzer
{
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
        RegistryDiagnostics.MultiplePrimaryResourceFiles
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

        // SPARK0201: Must implement IRegistry
        var implementsIRegistry = type.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.IRegistry");
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
        if (!string.IsNullOrWhiteSpace(identifier) && !IsSnakeCase(identifier!))
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
            if (!seenKeys.Add(key))
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
    }

    private static bool IsSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var ch in s)
        {
            if (ch == '_') continue;
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) continue;
            return false;
        }
        return true;
    }

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

        // Gather basic signature data
        var paramCount = method.Parameters.Length;
        var typeParamCount = method.TypeParameters.Length;

        // Too many type parameters
        if (typeParamCount > 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                RegistryDiagnostics.TooManyTypeParameters,
                attrLocation ?? method.Locations.FirstOrDefault(),
                method.Name));
            return; // don't flood with follow-up diagnostics
        }

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

        // If two params and one type param, the second param must be the generic type
        if (paramCount == 2)
        {
            var p1 = method.Parameters[1];
            if (typeParamCount == 1)
            {
                var tp = method.TypeParameters[0];
                if (!SymbolEqualityComparer.Default.Equals(p1.Type, tp))
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
}
