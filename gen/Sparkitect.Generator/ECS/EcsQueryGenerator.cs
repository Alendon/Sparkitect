using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.ECS;

/// <summary>
/// Roslyn incremental source generator that discovers <c>[ComponentQuery]</c> partial classes
/// and generates complete query implementations with typed entity handles.
/// </summary>
[Generator]
public class EcsQueryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline 1: Query class generation (Phase 42)
        // Scans ClassDeclarationSyntax for [ComponentQuery] partial classes.
        var queriesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: EcsQueryExtraction.TryExtractQueryModel
        ).Where(m => m is not null);

        context.RegisterSourceOutput(queriesProvider,
            static (ctx, model) => GenerateQueryClass(ctx, model!));

        // Pipeline 2: Resolution metadata generation (Phase 43)
        // Scans MethodDeclarationSyntax for SF-attributed methods with ComponentQuery parameters.
        // Standalone pipeline per D-04 -- no Combine with Pipeline 1.
        var buildSettings = context.GetModBuildSettings();

        var metadataProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: EcsMetadataExtraction.TryExtractEcsSystemMetadata
        ).Where(m => m is not null);

        context.RegisterSourceOutput(metadataProvider.Combine(buildSettings),
            static (ctx, pair) => GenerateMetadataEntrypoint(ctx, pair.Left!, pair.Right));
    }

    private static void GenerateMetadataEntrypoint(
        SourceProductionContext ctx, EcsSystemMetadataModel model, ModBuildSettings settings)
    {
        var models = model.QueryParameters
            .Select(qp => new EcsQueryMetadataModel(qp.QueryTypeFullyQualified))
            .Cast<IMetadataModel>()
            .ToList();

        if (DiPipeline.RenderMetadataEntrypoint(
                model.WrapperFullTypeName, model.WrapperTypeNamespace, models, settings,
                out var code, out var fileName))
        {
            ctx.AddSource(fileName, code);
        }
    }

    private static void GenerateQueryClass(
        SourceProductionContext ctx, EcsQueryModel model)
    {
        // Convert ComponentInfo records to anonymous types for Fluid template compatibility.
        // Fluid's UnsafeMemberAccessStrategy doesn't reliably access C# record properties;
        // anonymous types work correctly since Fluid can reflect their properties.
        static object ToFluid(ComponentInfo c) => new { c.FullyQualifiedName, c.ShortName };

        var readComponents = model.ReadComponents.Select(ToFluid).ToArray();
        var writeComponents = model.WriteComponents.Select(ToFluid).ToArray();
        var excludeComponents = model.ExcludeComponents.Select(ToFluid).ToArray();
        var allComponents = model.ReadComponents.Concat(model.WriteComponents).Select(ToFluid).ToArray();

        var templateModel = new
        {
            model.Namespace,
            model.ClassName,
            ReadComponents = readComponents,
            WriteComponents = writeComponents,
            ExcludeComponents = excludeComponents,
            AllComponents = allComponents,
            model.IsKeyed,
            model.KeyTypeFullyQualified,
            model.KeyTypeShort,
            model.KeyRequired
        };

        if (FluidHelper.TryRenderTemplate("ECS.ComponentQuery.liquid", templateModel, out var code))
        {
            ctx.AddSource($"{model.ClassName}.g.cs", code);
        }
    }
}
