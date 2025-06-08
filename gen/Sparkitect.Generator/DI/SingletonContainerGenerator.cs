using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.DI.DiUtils;

namespace Sparkitect.Generator.DI;

[Generator]
public class SingletonContainerGenerator : IIncrementalGenerator
{
    
    public void Initialize(IncrementalGeneratorInitializationContext genContext)
    {
        var buildSettings = genContext.GetModBuildSettings();        
        
        var singletonProvider = genContext.SyntaxProvider.ForAttributeWithMetadataName(SingletonAttributeMetadataName,
            (node, _) => node is ClassDeclarationSyntax, 
            (context, _) => context.TargetSymbol is INamedTypeSymbol symbol ? ExtractSingletonModel(symbol) : null)
            .NotNull()
            .Collect();

        genContext.RegisterSourceOutput(singletonProvider.Combine(buildSettings), (context, data) =>
        {
            var buildSettings = data.Right;
            var singletons = data.Left;
            
            // Skip generation if no singletons found
            if (singletons.IsEmpty)
                return;

            // Create container model
            var containerModel = CreateContainerModel(singletons, buildSettings);
            
            // Render and add source
            if (RenderCoreConfigurator(containerModel, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });
    }
    
    internal static SingletonModel ExtractSingletonModel(INamedTypeSymbol classSymbol)
    {
        // Generate the factory class name: ClassName + "_Factory"
        var factoryClassName = $"{classSymbol.Name}_Factory";
        
        // Generate the fully qualified factory name using global:: prefix
        var factoryFullName = $"global::{classSymbol.ContainingNamespace.ToDisplayString()}.{factoryClassName}";

        return new SingletonModel(factoryFullName);
    }

    internal static SingletonContainerModel CreateContainerModel(ImmutableArray<SingletonModel> singletons, ModBuildSettings buildSettings)
    {
        // TODO: Consider using ModName for configurator class name, but need to handle unsafe characters (spaces, special chars, etc.)
        // For now, use RootNamespace which should be a safe C# identifier
        
        // Generate configurator class name based on RootNamespace
        var configuratorClassName = string.IsNullOrEmpty(buildSettings.RootNamespace) 
            ? "GeneratedConfigurator" 
            : $"{buildSettings.RootNamespace}Configurator";

        // Use RootNamespace for namespace, fallback to "Generated" if empty
        var namespaceName = string.IsNullOrEmpty(buildSettings.RootNamespace) 
            ? "Generated" 
            : buildSettings.RootNamespace;

        return new SingletonContainerModel(
            configuratorClassName,
            namespaceName,
            singletons.ToValueCompareList());
    }

    internal static bool RenderCoreConfigurator(SingletonContainerModel model, out string code, out string fileName)
    {
        fileName = $"{model.ConfiguratorClassName}.g.cs";
        return FluidHelper.TryRenderTemplate("DI.CoreConfigurator.liquid", model, out code);
    }
}