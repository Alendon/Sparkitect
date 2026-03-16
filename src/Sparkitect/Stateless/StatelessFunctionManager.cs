using Sparkitect.DI;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

[StateService<IStatelessFunctionManager, CoreModule>]
internal sealed class StatelessFunctionManager : IStatelessFunctionManager
{
    private readonly Dictionary<Identification, Type> _wrapperTypes = new();

    public required IDIService DIService { get; init; }

    public void AddFunction<TStatelessFunction>(Identification id)
        where TStatelessFunction : IStatelessFunction
    {
        _wrapperTypes[id] = typeof(TStatelessFunction);
    }

    public IReadOnlyList<IStatelessFunction> GetSorted<TStatelessFunction, TContext, TRegistry>(
        IResolutionScope scope,
        TContext context,
        IEnumerable<string> loadedMods)
        where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry
    {
        var graphBuilder = CreateGraphBuilder();

        using var entrypointContainer = DIService.CreateEntrypointContainer<
            ApplySchedulingEntrypoint<TStatelessFunction, TContext, IExecutionGraphBuilder>>(loadedMods);

        entrypointContainer.ProcessMany(entrypoint =>
            entrypoint.BuildGraph(graphBuilder, context));

        var sortedIds = graphBuilder.Resolve();

        return InstantiateWrappers(sortedIds, scope);
    }

    public IReadOnlyList<IStatelessFunction> InstantiateWrappers(
        IReadOnlyList<Identification> sortedIds,
        IResolutionScope scope)
    {
        var result = new List<IStatelessFunction>(sortedIds.Count);
        foreach (var id in sortedIds)
        {
            if (!_wrapperTypes.TryGetValue(id, out var wrapperType))
            {
                throw new InvalidOperationException(
                    $"Wrapper type not found for function ID: {id}");
            }

            var wrapper = (IStatelessFunction)Activator.CreateInstance(wrapperType)!;
            wrapper.Initialize(scope);
            result.Add(wrapper);
        }

        return result;
    }

    public IReadOnlyCollection<Type> GetRegisteredWrapperTypes()
        => _wrapperTypes.Values.ToList();

    public IExecutionGraphBuilder CreateGraphBuilder() => new ExecutionGraphBuilder();
}
