using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.Metadata.Analyzers.MetadataDiagnostics;

namespace Sparkitect.Generator.Metadata.Analyzers;

/// <summary>
/// Flags metadata parameter attributes placed on a symbol that no present metadata category
/// harvests, deriving the legal placement set from the metadata model itself. A new category is
/// validated with no analyzer change: harvestability is read from category payload constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MetadataParameterAnalyzer : DiagnosticAnalyzer
{
    private const string MetadataAttributeBaseFqn = "Sparkitect.Metadata.MetadataAttribute";
    private const string MetadataParameterMarkerFqn = "Sparkitect.Metadata.MetadataParameterAttribute";
    private const string ExemptionFqn = "Sparkitect.Metadata.AllowUnharvestedMetadataParametersAttribute";
    private const string AttributeUsageFqn = "System.AttributeUsageAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [OrphanMetadataParameter];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext ctx)
    {
        var model = BuildModel(ctx.Compilation);
        ctx.RegisterSymbolAction(c => ValidateMethod(c, model), SymbolKind.Method);
        ctx.RegisterSymbolAction(c => ValidateType(c, model), SymbolKind.NamedType);
    }

    // Marker keys harvestable at each scope, unioned over every discovered category.
    private sealed class MetadataModel(ImmutableHashSet<string> atMethod, ImmutableHashSet<string> atClass)
    {
        public ImmutableHashSet<string> HarvestableAtMethod { get; } = atMethod;
        public ImmutableHashSet<string> HarvestableAtClass { get; } = atClass;
    }

    private static MetadataModel BuildModel(Compilation compilation)
    {
        var atMethod = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var atClass = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

        foreach (var category in EnumerateCategories(compilation))
        {
            var harvested = ComputeHarvestedMarkers(category);
            if (harvested.Count == 0)
                continue;

            var targets = ResolveTargets(category);
            if ((targets & AttributeTargets.Method) != 0)
                atMethod.UnionWith(harvested);
            if ((targets & AttributeTargets.Class) != 0)
                atClass.UnionWith(harvested);
        }

        return new MetadataModel(atMethod.ToImmutable(), atClass.ToImmutable());
    }

    // Categories = attribute types inheriting MetadataAttribute<T> with a concrete payload.
    private static IEnumerable<INamedTypeSymbol> EnumerateCategories(Compilation compilation)
    {
        foreach (var type in EnumerateAllTypes(compilation.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class)
                continue;

            var genericBase = MetadataExtractionPipeline.FindGenericBase(type, MetadataAttributeBaseFqn);
            if (genericBase is { TypeArguments.Length: 1 } && genericBase.TypeArguments[0] is INamedTypeSymbol)
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                foreach (var nested in EnumerateAllTypes(childNs))
                    yield return nested;
            }
            else if (member is INamedTypeSymbol type)
            {
                foreach (var nested in EnumerateNested(type))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
        foreach (var inner in EnumerateNested(nested))
            yield return inner;
    }

    // A category's target kinds come from the nearest AttributeUsage in its base chain
    // (GetAttributes returns only directly-applied attributes); default to all targets.
    private static AttributeTargets ResolveTargets(INamedTypeSymbol category)
    {
        for (var type = category; type is not null; type = type.BaseType)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) != AttributeUsageFqn)
                    continue;
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int flags)
                    return (AttributeTargets)flags;
            }
        }

        return AttributeTargets.All;
    }

    // Marker keys a category harvests = its payload ctor params whose element type is a parameter marker.
    private static ImmutableHashSet<string> ComputeHarvestedMarkers(INamedTypeSymbol categoryAttrClass)
    {
        var genericBase = MetadataExtractionPipeline.FindGenericBase(categoryAttrClass, MetadataAttributeBaseFqn);
        if (genericBase is not { TypeArguments.Length: 1 } ||
            genericBase.TypeArguments[0] is not INamedTypeSymbol payload)
            return ImmutableHashSet<string>.Empty;

        var ctor = payload.Constructors.FirstOrDefault(c => !c.IsStatic);
        if (ctor is null)
            return ImmutableHashSet<string>.Empty;

        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var param in ctor.Parameters)
        {
            var elementType = param.Type is IArrayTypeSymbol array ? array.ElementType : param.Type;
            if (elementType is INamedTypeSymbol named &&
                MetadataExtractionPipeline.InheritsFrom(named, MetadataParameterMarkerFqn))
                builder.Add(MetadataExtractionPipeline.GetNonGenericBaseTypeName(elementType));
        }

        return builder.ToImmutable();
    }

    private static void ValidateMethod(SymbolAnalysisContext ctx, MetadataModel model)
    {
        if (ctx.Symbol is not IMethodSymbol method)
            return;
        if (method.ContainingType is { } containing && HasExemption(containing))
            return;

        Validate(ctx, method, model.HarvestableAtMethod);
    }

    private static void ValidateType(SymbolAnalysisContext ctx, MetadataModel model)
    {
        if (ctx.Symbol is not INamedTypeSymbol type)
            return;
        if (HasExemption(type))
            return;

        Validate(ctx, type, model.HarvestableAtClass);
    }

    private static void Validate(SymbolAnalysisContext ctx, ISymbol symbol, ImmutableHashSet<string> harvestableHere)
    {
        if (harvestableHere.IsEmpty)
            return;

        var present = ComputePresentHarvested(symbol);

        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null || !MetadataExtractionPipeline.InheritsFrom(attrClass, MetadataParameterMarkerFqn))
                continue;

            var key = MetadataExtractionPipeline.GetNonGenericBaseTypeName(attrClass);
            if (!harvestableHere.Contains(key) || present.Contains(key))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                OrphanMetadataParameter,
                GetAttributeLocation(attr) ?? symbol.Locations.FirstOrDefault(),
                $"[{attrClass.Name}]",
                symbol.Name));
        }
    }

    // Marker keys already harvested by categories actually present on the symbol.
    private static ImmutableHashSet<string> ComputePresentHarvested(ISymbol symbol)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null ||
                MetadataExtractionPipeline.FindGenericBase(attrClass, MetadataAttributeBaseFqn) is null)
                continue;
            builder.UnionWith(ComputeHarvestedMarkers(attrClass));
        }

        return builder.ToImmutable();
    }

    private static bool HasExemption(INamedTypeSymbol type) =>
        type.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == ExemptionFqn);

    private static Location? GetAttributeLocation(AttributeData attr) =>
        attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
}
