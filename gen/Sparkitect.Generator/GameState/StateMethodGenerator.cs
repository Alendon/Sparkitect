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

                return ExtractStateModuleModel(classSymbol, syntaxContext.SemanticModel.Compilation, cancellationToken);
            }).Where(m => m is not null)!;

        // Collect all modules for association and ordering generation
        var allModulesProvider = stateModulesProvider.Collect();

        // Register output for individual module wrapper classes
        context.RegisterSourceOutput(stateModulesProvider, (context, model) =>
        {
            if (model is null) return;

            foreach (var function in model.Functions)
            {
                if (RenderStateMethodWrapper(model, function, out var code, out var fileName))
                {
                    context.AddSource(fileName, code);
                }
            }
        });

        // Register output for StateMethodAssociation configurator
        context.RegisterSourceOutput(allModulesProvider, (context, modules) =>
        {
            if (modules.Length == 0) return;

            if (RenderStateMethodAssociation(modules, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });

        // Register output for StateMethodOrdering configurator
        context.RegisterSourceOutput(allModulesProvider, (context, modules) =>
        {
            if (modules.Length == 0) return;

            if (RenderStateMethodOrdering(modules, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });
    }

    internal static StateModuleModel? ExtractStateModuleModel(INamedTypeSymbol moduleType, Compilation compilation, System.Threading.CancellationToken cancellationToken)
    {
        // Get module identification
        var identificationProperty = moduleType.GetMembers("Identification").OfType<IPropertySymbol>().FirstOrDefault();
        if (identificationProperty is null)
            return null;

        // Extract functions
        var functions = new List<StateFunctionModel>();
        var methods = moduleType.GetMembers().OfType<IMethodSymbol>();

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
                key,
                schedule.Value,
                parameters,
                orderingConstraints));
        }

        if (functions.Count == 0)
            return null;

        // Get module ordering
        var (moduleBefore, moduleAfter) = GetModuleOrderingConstraints(moduleType);

        // Generate property access expression for Identification (not the type)
        var identificationExpression = $"{moduleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{identificationProperty.Name}";

        return new StateModuleModel(
            moduleType.Name,
            moduleType.ContainingNamespace.ToDisplayString(),
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