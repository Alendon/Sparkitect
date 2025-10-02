using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.GameState.Diagnostics;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.GameState.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateOrderingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        ModuleOrderingCycleDetected,
        OrderingCycleDetected
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Register compilation-end action to analyze module ordering across all modules
        context.RegisterCompilationAction(AnalyzeModuleOrdering);
    }

    private void AnalyzeModuleOrdering(CompilationAnalysisContext context)
    {
        // Collect all state modules in the compilation
        var modules = new List<(INamedTypeSymbol Type, string[] Before, string[] After)>();

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot(context.CancellationToken);

            foreach (var node in root.DescendantNodes())
            {
                if (semanticModel.GetDeclaredSymbol(node, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
                    continue;

                if (!IsStateModule(typeSymbol))
                    continue;

                var (beforeTypes, afterTypes) = GetModuleOrderingConstraints(typeSymbol);

                modules.Add((typeSymbol, beforeTypes.ToArray(), afterTypes.ToArray()));
            }
        }

        if (modules.Count == 0)
            return;

        // Build dependency graph
        var graph = new Dictionary<string, HashSet<string>>();
        var modulesByName = modules.ToDictionary(
            m => m.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            m => m.Type);

        foreach (var (type, before, after) in modules)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (!graph.ContainsKey(typeName))
                graph[typeName] = new HashSet<string>();

            // If A orders before B, then B depends on A (A must come first)
            // So we add edges: B -> A for each "before"
            foreach (var beforeType in before)
            {
                if (!graph.ContainsKey(beforeType))
                    graph[beforeType] = new HashSet<string>();

                // beforeType must come before current type
                // So current type depends on beforeType
                graph[typeName].Add(beforeType);
            }

            // If A orders after B, then A depends on B (B must come first)
            // So we add edges: A -> B for each "after"
            foreach (var afterType in after)
            {
                if (!graph.ContainsKey(afterType))
                    graph[afterType] = new HashSet<string>();

                // Current type must come after afterType
                // So current type depends on afterType
                graph[typeName].Add(afterType);
            }
        }

        // Detect cycles using DFS
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (DetectCycle(node, graph, visited, recursionStack, out var cycle))
            {
                // Report diagnostic on the first module in the cycle
                if (modulesByName.TryGetValue(cycle.First(), out var moduleType))
                {
                    var cycleDescription = string.Join(" -> ", cycle);
                    context.ReportDiagnostic(Diagnostic.Create(
                        ModuleOrderingCycleDetected,
                        moduleType.Locations.FirstOrDefault(),
                        cycleDescription));
                }

                break; // Report only the first cycle found
            }
        }
    }

    private bool DetectCycle(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        out List<string> cycle)
    {
        cycle = new List<string>();

        if (recursionStack.Contains(node))
        {
            // Cycle detected
            cycle.Add(node);
            return true;
        }

        if (visited.Contains(node))
            return false;

        visited.Add(node);
        recursionStack.Add(node);

        if (graph.TryGetValue(node, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (DetectCycle(dependency, graph, visited, recursionStack, out cycle))
                {
                    if (cycle.Count == 0 || cycle.First() != node)
                    {
                        cycle.Insert(0, node);
                    }
                    return true;
                }
            }
        }

        recursionStack.Remove(node);
        return false;
    }
}