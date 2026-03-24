using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.Modding;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Metadata;

[Generator]
public class MetadataGenerator : IIncrementalGenerator
{
    private const string MetadataCategoryMarkerFqn = "Sparkitect.Metadata.MetadataCategoryMarkerAttribute";
    private const string MetadataAttributeBaseFqn = "Sparkitect.Metadata.MetadataAttribute";
    private const string IHasIdentificationFqn = "Sparkitect.Modding.IHasIdentification";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        var typesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: (ctx, ct) => TryExtractMetadataTarget(ctx)
        ).Where(m => m is not null);

        context.RegisterSourceOutput(typesProvider.Combine(buildSettings),
            static (ctx, pair) => OutputMetadataEntrypoint(ctx, pair.Left!, pair.Right));
    }

    private static MetadataTargetModel? TryExtractMetadataTarget(GeneratorSyntaxContext syntaxContext)
    {
        if (syntaxContext.Node is not ClassDeclarationSyntax)
            return null;

        var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) as INamedTypeSymbol;
        if (symbol is null)
            return null;

        // Exclude compiler-generated classes to avoid infinite loops
        if (symbol.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) ==
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            return null;

        // Check implements IHasIdentification
        if (!symbol.AllInterfaces.Any(i =>
                i.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationFqn))
            return null;

        // Scan attributes for MetadataCategoryMarker
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass is null)
                continue;

            // Check if the attribute's CLASS has [MetadataCategoryMarker]
            if (!attr.AttributeClass.GetAttributes().Any(a =>
                    a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == MetadataCategoryMarkerFqn))
                continue;

            // Walk base chain to find MetadataAttribute<T>
            var genericBase = MetadataExtractionPipeline.FindGenericBase(attr.AttributeClass, MetadataAttributeBaseFqn);
            if (genericBase is not { TypeArguments.Length: 1 })
                continue;

            var metadataType = genericBase.TypeArguments[0] as INamedTypeSymbol;
            if (metadataType is null)
                continue;

            // Extract constructor params (no typeArgumentResolver for type-level metadata)
            var constructorParams = MetadataExtractionPipeline.Extract(metadataType, symbol);

            var typeFullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeShortName = symbol.Name;
            var typeNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var metadataTypeName = metadataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            return new MetadataTargetModel(
                typeFullName,
                typeShortName,
                typeNamespace,
                metadataTypeName,
                constructorParams);
        }

        return null;
    }

    private static void OutputMetadataEntrypoint(
        SourceProductionContext context,
        MetadataTargetModel model,
        ModBuildSettings settings)
    {
        var className = $"{model.TypeShortName}_Metadata";

        var templateModel = new
        {
            Namespace = settings.ComputeOutputNamespace(),
            ClassName = className,
            model.MetadataTypeName,
            model.TypeFullName,
            ConstructorParams = model.ConstructorParams.Select(p => new
            {
                p.AttributeTypeName,
                p.IsNullable,
                p.IsArray,
                Instances = p.Instances.Select(inst => new
                {
                    GenericArgs = inst.GenericArgs.ToArray(),
                    CtorArgs = inst.CtorArgs.ToArray()
                }).ToArray()
            }).ToArray()
        };

        if (FluidHelper.TryRenderTemplate("Metadata.MetadataEntrypoint.liquid", templateModel, out var code))
        {
            context.AddSource($"{model.TypeShortName}_Metadata.g.cs", code);
        }
    }
}
