using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.GameState;

[Generator]
public class StateMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types implementing IStateModule
        var stateModulesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            transform: (syntaxContext, cancellationToken) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, cancellationToken) is not INamedTypeSymbol classSymbol)
                    return null;

                if (!IsStateModule(classSymbol))
                    return null;

                return ExtractStateParentModel(classSymbol, syntaxContext.SemanticModel.Compilation, cancellationToken);
            }).Where(m => m is not null);

        // Find all types implementing IStateDescriptor
        var stateDescriptorsProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            transform: (syntaxContext, cancellationToken) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, cancellationToken) is not INamedTypeSymbol classSymbol)
                    return null;

                if (!IsStateDescriptor(classSymbol))
                    return null;

                return ExtractStateParentModel(classSymbol, syntaxContext.SemanticModel.Compilation, cancellationToken);
            }).Where(m => m is not null)!;

        // Combine modules and descriptors
        var allParentsProvider = stateModulesProvider
            .Collect()
            .Combine(stateDescriptorsProvider.Collect())
            .Select((pair, _) => pair.Left.Concat(pair.Right).Where(x => x is not null).Select<StateModuleModel?, StateModuleModel>( x => x!).ToImmutableArray());

        // Register output for individual module wrapper classes
        context.RegisterSourceOutput(stateModulesProvider, (productionContext, model) =>
        {
            if (model is null) return;

            foreach (var function in model.Functions)
            {
                if (RenderStateMethodWrapper(model, function, out var code, out var fileName))
                {
                    productionContext.AddSource(fileName, code);
                }
            }
        });

        // Register output for individual descriptor wrapper classes
        context.RegisterSourceOutput(stateDescriptorsProvider, (productionContext, model) =>
        {
            if (model is null) return;

            foreach (var function in model.Functions)
            {
                if (RenderStateMethodWrapper(model, function, out var code, out var fileName))
                {
                    productionContext.AddSource(fileName, code);
                }
            }
        });

        // Register output for StateMethodAssociation configurator
        context.RegisterSourceOutput(allParentsProvider, (productionContext, parents) =>
        {
            if (parents.Length == 0) return;

            if (RenderStateMethodAssociation(parents, out var code, out var fileName))
            {
                productionContext.AddSource(fileName, code);
            }
        });

        // Register output for StateMethodOrdering configurator
        context.RegisterSourceOutput(allParentsProvider, (productionContext, parents) =>
        {
            if (parents.Length == 0) return;

            if (RenderStateMethodOrdering(parents, out var code, out var fileName))
            {
                productionContext.AddSource(fileName, code);
            }
        });
    }

    internal static StateModuleModel? ExtractStateParentModel(INamedTypeSymbol parentType, Compilation compilation, System.Threading.CancellationToken cancellationToken)
    {
        // Get parent identification (module or descriptor)
        var identificationProperty = parentType.GetMembers("Identification").OfType<IPropertySymbol>().FirstOrDefault();
        if (identificationProperty is null)
            return null;

        // Extract functions
        var functions = new List<StateFunctionModel>();
        var methods = parentType.GetMembers().OfType<IMethodSymbol>();

        foreach (var method in methods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stateFunctionAttr = GetStateFunctionAttribute(method);
            if (stateFunctionAttr is null)
                continue;

            var key = GetFunctionKey(stateFunctionAttr);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var schedule = GetScheduleFromAttributes(method);
            if (schedule is null)
                continue;

            // Extract parameters
            var parameters = method.Parameters
                .Select(p => new StateParameterModel(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.NullableAnnotation == NullableAnnotation.Annotated))
                .ToImmutableValueArray();

            // Extract ordering constraints
            var orderingConstraints = GetOrderingConstraints(method).ToImmutableValueArray();

            functions.Add(new StateFunctionModel(
                method.Name,
                key!,
                schedule.Value,
                parameters,
                orderingConstraints));
        }

        if (functions.Count == 0)
            return null;

        // Get module ordering
        var (moduleBefore, moduleAfter) = GetModuleOrderingConstraints(parentType);

        // Generate property access expression for Identification (not the type)
        var identificationExpression = $"{parentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{identificationProperty.Name}";

        return new StateModuleModel(
            parentType.Name,
            parentType.ContainingNamespace.ToDisplayString(),
            identificationExpression,
            functions.ToImmutableValueArray(),
            moduleBefore.ToImmutableValueArray(),
            moduleAfter.ToImmutableValueArray());
    }

    internal static bool RenderStateMethodWrapper(StateModuleModel module, StateFunctionModel function, out string code, out string fileName)
    {
        fileName = $"{module.ModuleTypeName}_{function.FunctionKey}_Wrapper.g.cs";

        var model = new
        {
            module.ModuleNamespace,
            module.ModuleTypeName,
            function.MethodName,
            function.FunctionKey,
            Parameters = function.Parameters.Select((p, i) => new
            {
                Index = i,
                p.ParameterName,
                p.ParameterType,
                p.IsOptional
            }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("GameState.StateMethodWrapper.liquid", model, out code);
    }

    internal static bool RenderStateMethodAssociation(ImmutableArray<StateModuleModel> modules, out string code, out string fileName)
    {
        fileName = "StateMethodAssociation.g.cs";

        // Sort modules and their functions for determinism
        var sortedModules = modules.OrderBy(m => m.ModuleTypeName).ToArray();

        var registrations = new List<StateMethodRegistration>();

        foreach (var module in sortedModules)
        {
            var sortedFunctions = module.Functions.OrderBy(f => f.FunctionKey).ToArray();

            foreach (var function in sortedFunctions)
            {
                registrations.Add(new StateMethodRegistration(
                    $"global::{module.ModuleNamespace}.{module.ModuleTypeName}",
                    module.ModuleIdentification,
                    function.FunctionKey,
                    $"global::{module.ModuleNamespace}.{module.ModuleTypeName}.{function.FunctionKey}Wrapper",
                    function.Schedule.ToString()));
            }
        }

        // Determine output namespace (use first module's namespace or fallback)
        var outputNamespace = sortedModules.FirstOrDefault()?.ModuleNamespace ?? "Sparkitect.CompilerGenerated";

        var model = new StateMethodAssociationModel(
            outputNamespace,
            "GeneratedStateMethodAssociation",
            registrations.ToImmutableValueArray());

        return FluidHelper.TryRenderTemplate("GameState.StateMethodAssociation.liquid", model, out code);
    }

    internal static bool RenderStateMethodOrdering(ImmutableArray<StateModuleModel> modules, out string code, out string fileName)
    {
        fileName = "StateMethodOrdering.g.cs";

        // Sort modules for determinism
        var sortedModules = modules.OrderBy(m => m.ModuleTypeName).ToArray();

        var orderings = new List<OrderingRelationship>();

        foreach (var module in sortedModules)
        {
            var sortedFunctions = module.Functions.OrderBy(f => f.FunctionKey).ToArray();

            foreach (var function in sortedFunctions)
            {
                foreach (var constraint in function.OrderingConstraints.OrderBy(c => c.TargetKey))
                {
                    // Determine parent ID for the constraint
                    string targetParentId = constraint.TargetModuleOrStateType ?? module.ModuleIdentification;

                    OrderingRelationship relationship;

                    if (constraint.Direction == OrderingDirection.Before)
                    {
                        // This function should run before the target
                        // So: target comes after this
                        relationship = new OrderingRelationship(
                            module.ModuleIdentification,
                            function.FunctionKey,
                            targetParentId,
                            constraint.TargetKey);
                    }
                    else // After
                    {
                        // This function should run after the target
                        // So: this comes after target
                        relationship = new OrderingRelationship(
                            targetParentId,
                            constraint.TargetKey,
                            module.ModuleIdentification,
                            function.FunctionKey);
                    }

                    orderings.Add(relationship);
                }
            }
        }

        // Determine output namespace
        var outputNamespace = sortedModules.FirstOrDefault()?.ModuleNamespace ?? "Sparkitect.CompilerGenerated";

        var model = new StateMethodOrderingModel(
            outputNamespace,
            "GeneratedStateMethodOrdering",
            orderings.ToImmutableValueArray());

        return FluidHelper.TryRenderTemplate("GameState.StateMethodOrdering.liquid", model, out code);
    }
}