using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Modding.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistryProviderUsageAnalyzer : DiagnosticAnalyzer
{
    private static Location? GetAttributeLocation(AttributeData attr)
    {
        return attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        RegistryDiagnostics.ProviderMissingId,
        RegistryDiagnostics.ProviderMemberMustBeStatic,
        RegistryDiagnostics.UnknownRegistryReference,
        RegistryDiagnostics.UnknownRegistryMethod,
        RegistryDiagnostics.ProviderKindMismatch,
        RegistryDiagnostics.ProviderReturnTypeIncompatible,
        RegistryDiagnostics.TypeDoesNotSatisfyConstraints,
        RegistryDiagnostics.DuplicateRegistrationId,
        RegistryDiagnostics.RegistrationIdNotSnakeCase,
        RegistryDiagnostics.DiParameterShouldBeAbstract,
        RegistryDiagnostics.DuplicateNormalizedPropertyName
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Per-attribute immediate checks
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);

        // Aggregated checks across the compilation (duplicate ids and normalized names)
        context.RegisterCompilationStartAction(startCtx =>
        {
            var byRegistry = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentBag<Seen>>();

            startCtx.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node is not AttributeSyntax attrSyntax) return;

                var decl = attrSyntax.Parent?.Parent;
                if (decl is null) return;
                ISymbol? targetSymbol = decl switch
                {
                    MethodDeclarationSyntax mds => ctx.SemanticModel.GetDeclaredSymbol(mds, ctx.CancellationToken),
                    PropertyDeclarationSyntax pds => ctx.SemanticModel.GetDeclaredSymbol(pds, ctx.CancellationToken),
                    ClassDeclarationSyntax cds => ctx.SemanticModel.GetDeclaredSymbol(cds, ctx.CancellationToken),
                    _ => null
                };
                if (targetSymbol is null) return;

                var attrData = targetSymbol.GetAttributes().FirstOrDefault(a =>
                    a.ApplicationSyntaxReference?.Span.Equals(attrSyntax.Span) == true);
                if (attrData is null) return;

                if (!RegistryGenerator.TryExtractProviderInfo(attrData, out var regTypeName, out var registryNamespace,
                        out var methodName, out _))
                    return;

                if (!RegistryGenerator.TryParseProviderArguments(attrSyntax, out var id, out _))
                    return; // no id -> nothing to aggregate

                var comp = ctx.SemanticModel.Compilation;
                INamedTypeSymbol? registryType = null;
                if (!string.IsNullOrWhiteSpace(regTypeName))
                {
                    if (!string.IsNullOrWhiteSpace(registryNamespace))
                    {
                        registryType = comp.GetTypeByMetadataName($"{registryNamespace}.{regTypeName}");
                    }
                    registryType ??= comp.GlobalNamespace.GetNamespaceMembers()
                        .SelectMany(AllTypes)
                        .FirstOrDefault(t => t.Name == regTypeName && t.AllInterfaces.Any(i =>
                            i.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.IRegistry"));
                }
                if (registryType is null) return;

                var regKey = registryType.ContainingNamespace?.ToDisplayString() is { Length: > 0 } ns
                    ? $"{ns}.{registryType.Name}"
                    : registryType.Name;

                var pascal = Sparkitect.Generator.Modding.RegistryGenerator.ToPascalCase(id);

                var bag = byRegistry.GetOrAdd(regKey, _ => new System.Collections.Concurrent.ConcurrentBag<Seen>());
                bag.Add(new Seen(id, pascal, attrSyntax.GetLocation(), registryType.Name));

            }, SyntaxKind.Attribute);

            startCtx.RegisterCompilationEndAction(endCtx =>
            {
                foreach (var kv in byRegistry)
                {
                    var items = kv.Value.ToArray();
                    var registryName = items.FirstOrDefault().RegistryName ?? kv.Key;

                    // SPARK0230: duplicate ids within registry
                    foreach (var grp in items.GroupBy(x => x.Id))
                    {
                        if (grp.Count() <= 1) continue;
                        var first = grp.First();
                        foreach (var dup in grp.Skip(1))
                        {
                            endCtx.ReportDiagnostic(Diagnostic.Create(
                                RegistryDiagnostics.DuplicateRegistrationId,
                                dup.Location,
                                dup.Id,
                                registryName));
                        }
                    }

                    // SPARK0250: duplicate normalized property names
                    foreach (var grp in items.GroupBy(x => x.Pascal))
                    {
                        if (grp.Count() <= 1) continue;
                        var first = grp.First();
                        foreach (var dup in grp.Skip(1))
                        {
                            endCtx.ReportDiagnostic(Diagnostic.Create(
                                RegistryDiagnostics.DuplicateNormalizedPropertyName,
                                dup.Location,
                                first.Id,
                                dup.Id,
                                grp.Key,
                                registryName));
                        }
                    }
                }
            });
        });
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.Node is not AttributeSyntax attrSyntax) return;

        // Determine declaration target
        var decl = attrSyntax.Parent?.Parent;
        if (decl is null) return;

        ISymbol? targetSymbol = decl switch
        {
            MethodDeclarationSyntax mds => ctx.SemanticModel.GetDeclaredSymbol(mds, ctx.CancellationToken),
            PropertyDeclarationSyntax pds => ctx.SemanticModel.GetDeclaredSymbol(pds, ctx.CancellationToken),
            ClassDeclarationSyntax cds => ctx.SemanticModel.GetDeclaredSymbol(cds, ctx.CancellationToken),
            _ => null
        };
        if (targetSymbol is null) return;

        // Bind to the exact AttributeData for this syntax instance
        var attrData = targetSymbol.GetAttributes().FirstOrDefault(a =>
            a.ApplicationSyntaxReference?.Span.Equals(attrSyntax.Span) == true);
        if (attrData is null) return;

        // Recognize registry provider attributes (generated nested attribute or error symbol)
        if (!RegistryGenerator.TryExtractProviderInfo(attrData, out var regTypeName, out var registryNamespace,
                out var methodName, out _))
            return;

        // SPARK0220: parse id; if absent -> diagnostic
        if (!RegistryGenerator.TryParseProviderArguments(attrSyntax, out var id, out _))
        {
            Report(ctx, RegistryDiagnostics.ProviderMissingId, attrSyntax.GetLocation(), attrSyntax.Name.ToString());
            return; // further checks rely on an id
        }

        // SPARK0231: id must be snake_case
        if (!IsSnakeCase(id))
        {
            Report(ctx, RegistryDiagnostics.RegistrationIdNotSnakeCase, attrSyntax.GetLocation(), id);
        }

        // SPARK0221: provider member must be static (methods/properties)
        // Report at the provider attribute location
        if (targetSymbol is IMethodSymbol ms && !ms.IsStatic)
        {
            var memberAttrLocation = GetAttributeLocation(attrData);
            Report(ctx, RegistryDiagnostics.ProviderMemberMustBeStatic, memberAttrLocation ?? ms.Locations.FirstOrDefault(), ms.Name);
        }
        else if (targetSymbol is IPropertySymbol ps && !ps.IsStatic)
        {
            var memberAttrLocation = GetAttributeLocation(attrData);
            Report(ctx, RegistryDiagnostics.ProviderMemberMustBeStatic, memberAttrLocation ?? ps.Locations.FirstOrDefault(), ps.Name);
        }

        // SPARK0222: Unknown registry (not discoverable)
        var comp = ctx.SemanticModel.Compilation;
        INamedTypeSymbol? registryType = null;
        if (!string.IsNullOrWhiteSpace(regTypeName))
        {
            if (!string.IsNullOrWhiteSpace(registryNamespace))
            {
                registryType = comp.GetTypeByMetadataName($"{registryNamespace}.{regTypeName}");
            }

            // Fallback: search by name among types that implement IRegistry
            registryType ??= comp.GlobalNamespace.GetNamespaceMembers()
                .SelectMany(AllTypes)
                .FirstOrDefault(t => t.Name == regTypeName && t.AllInterfaces.Any(i =>
                    i.ToDisplayString(DisplayFormats.NamespaceAndType) == "Sparkitect.Modding.IRegistry"));
        }

        if (registryType is null)
        {
            Report(ctx, RegistryDiagnostics.UnknownRegistryReference, attrSyntax.GetLocation(), regTypeName);
            return; // can't check method without registry
        }

        // SPARK0223: Unknown registry method name
        var hasMethod = registryType.GetMembers().OfType<IMethodSymbol>().Any(m =>
            m.Name == methodName && m.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                "Sparkitect.Modding.RegistryMethodAttribute"));

        IMethodSymbol? registryMethod = null;
        if (!hasMethod)
        {
            Report(ctx, RegistryDiagnostics.UnknownRegistryMethod, attrSyntax.GetLocation(), registryType.Name, methodName);
            return;
        }
        else
        {
            registryMethod = registryType.GetMembers().OfType<IMethodSymbol>().First(m =>
                m.Name == methodName && m.GetAttributes().Any(a =>
                    a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                    "Sparkitect.Modding.RegistryMethodAttribute"));
        }

        // Determine method kind by signature
        var methodKind = GetRegistryMethodKind(registryMethod);
        var usageKind = (targetSymbol is INamedTypeSymbol) ? ProviderUsageKind.Type : ProviderUsageKind.Value;

        // SPARK0224: kind mismatch
        if (methodKind == PrimaryKind.None || (usageKind == ProviderUsageKind.Type && methodKind != PrimaryKind.Type) ||
            (usageKind == ProviderUsageKind.Value && methodKind == PrimaryKind.Type))
        {
            Report(ctx, RegistryDiagnostics.ProviderKindMismatch, attrSyntax.GetLocation(), attrSyntax.Name.ToString(), usageKind.ToString(), methodKind.ToString());
        }

        // SPARK0225: return type incompatible for non-generic value methods
        if (usageKind == ProviderUsageKind.Value && methodKind == PrimaryKind.Value)
        {
            var expected = registryMethod.Parameters.Length >= 2 ? registryMethod.Parameters[1].Type : null;
            if (expected != null)
            {
                ITypeSymbol? providedType = null;
                if (targetSymbol is IMethodSymbol pms)
                {
                    providedType = pms.ReturnType;
                }
                else if (targetSymbol is IPropertySymbol pps)
                {
                    providedType = pps.Type;
                }

                if (providedType != null)
                {
                    // Simple check: require identical display names (covers obvious mismatches like int vs string)
                    var a = providedType.ToDisplayString(DisplayFormats.NamespaceAndType);
                    var b = expected.ToDisplayString(DisplayFormats.NamespaceAndType);
                    if (a != b)
                    {
                        Report(ctx, RegistryDiagnostics.ProviderReturnTypeIncompatible, attrSyntax.GetLocation(), targetSymbol.Name, a, registryMethod.Name);
                    }
                }
            }
        }

        // SPARK0226: generic constraints for Type and GenericValue
        if (registryMethod.TypeParameters.Length == 1)
        {
            var tp = registryMethod.TypeParameters[0];
            ITypeSymbol? candidate = null;
            if (methodKind == PrimaryKind.Type && targetSymbol is INamedTypeSymbol typeProvider)
            {
                candidate = typeProvider;
            }
            else if (methodKind == PrimaryKind.GenericValue)
            {
                if (targetSymbol is IMethodSymbol pms)
                    candidate = pms.ReturnType;
                else if (targetSymbol is IPropertySymbol pps)
                    candidate = pps.Type;
            }

            if (candidate != null && !SatisfiesConstraints(ctx.SemanticModel.Compilation, candidate, tp))
            {
                Report(ctx, RegistryDiagnostics.TypeDoesNotSatisfyConstraints, attrSyntax.GetLocation(), candidate.ToDisplayString(DisplayFormats.NamespaceAndType), registryMethod.Name);
            }
        }

        // SPARK0232: DI parameter guidance for provider methods
        // Report at the provider attribute location for consistency
        if (targetSymbol is IMethodSymbol providerMethod)
        {
            var paramAttrLocation = GetAttributeLocation(attrData);
            foreach (var p in providerMethod.Parameters)
            {
                if (p.Type is INamedTypeSymbol pt)
                {
                    var isAbstractOrInterface = pt.TypeKind == TypeKind.Interface || pt.IsAbstract;
                    var isNullable = p.NullableAnnotation == NullableAnnotation.Annotated;
                    if (!isAbstractOrInterface && !isNullable)
                    {
                        Report(ctx, RegistryDiagnostics.DiParameterShouldBeAbstract, paramAttrLocation ?? p.Locations.FirstOrDefault(), p.Name, pt.ToDisplayString(DisplayFormats.NamespaceAndType));
                    }
                }
            }
        }
    }

    private static void Report(SyntaxNodeAnalysisContext ctx, DiagnosticDescriptor desc, Location? loc, params object[] args)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(desc, loc ?? Location.None, args));
    }

    private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns)
    {
        foreach (var t in ns.GetTypeMembers())
            yield return t;
        foreach (var n in ns.GetNamespaceMembers())
        {
            foreach (var t in AllTypes(n))
                yield return t;
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

    private enum ProviderUsageKind { Value, Type }

    private enum PrimaryKind { None, Value, GenericValue, Type }

    private static PrimaryKind GetRegistryMethodKind(IMethodSymbol m)
    {
        if (m.Parameters.Length == 1 && m.TypeParameters.Length == 0)
            return PrimaryKind.None;
        if (m.Parameters.Length == 2 && m.TypeParameters.Length == 0)
            return PrimaryKind.Value;
        if (m.Parameters.Length == 2 && m.TypeParameters.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, m.TypeParameters[0]))
            return PrimaryKind.GenericValue;
        if (m.Parameters.Length == 1 && m.TypeParameters.Length == 1)
            return PrimaryKind.Type;
        return PrimaryKind.Value; // fallback
    }

    private static bool SatisfiesConstraints(Compilation compilation, ITypeSymbol candidate, ITypeParameterSymbol tp)
    {
        // Class/struct constraints
        if (tp.HasReferenceTypeConstraint && candidate.IsValueType) return false;
        if (tp.HasValueTypeConstraint && !candidate.IsValueType) return false;

        // new() constraint
        if (tp.HasConstructorConstraint)
        {
            if (candidate is INamedTypeSymbol nts)
            {
                var hasPublicParameterlessCtor = nts.InstanceConstructors.Any(ctor =>
                    !ctor.IsStatic && ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public);
                if (!hasPublicParameterlessCtor) return false;
            }
        }

        // Specific type constraints: candidate must be implicitly convertible to each
        foreach (var ct in tp.ConstraintTypes)
        {
            // Simple check via inheritance/interface
            if (!IsAssignableTo(compilation, candidate, ct))
                return false;
        }

        return true;
    }

    private static bool IsAssignableTo(Compilation compilation, ITypeSymbol source, ITypeSymbol target)
    {
        // Identity
        if (SymbolEqualityComparer.Default.Equals(source, target)) return true;

        // Interface / base types
        if (source is INamedTypeSymbol named)
        {
            foreach (var i in named.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(i, target)) return true;

            var bt = named.BaseType;
            while (bt is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(bt, target)) return true;
                bt = bt.BaseType;
            }
        }

        return false;
    }

    private readonly record struct Seen(string Id, string Pascal, Location Location, string RegistryName);
}
