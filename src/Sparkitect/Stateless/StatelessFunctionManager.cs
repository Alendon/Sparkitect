using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

[CreateServiceFactory<IStatelessFunctionManager>]
internal sealed class StatelessFunctionManager : IStatelessFunctionManager
{
    private readonly Dictionary<Identification, Type> _wrapperTypes = new();

    public required IModDIService ModDIService { get; init; }

    public void AddFunction<TStatelessFunction>(Identification id)
        where TStatelessFunction : IStatelessFunction
    {
        _wrapperTypes[id] = typeof(TStatelessFunction);
    }

    public IReadOnlyList<IStatelessFunction> GetSorted<TStatelessFunction, TContext, TRegistry>(
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap,
        TContext context,
        IEnumerable<string> loadedMods)
        where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry
    {
        var graphBuilder = CreateGraphBuilder();

        using var entrypointContainer = ModDIService.CreateEntrypointContainer<
            ApplySchedulingEntrypoint<TStatelessFunction, TContext>>(loadedMods);

        entrypointContainer.ProcessMany(entrypoint =>
            entrypoint.BuildGraph(graphBuilder, context));

        var sortedIds = graphBuilder.Resolve();

        var result = new List<IStatelessFunction>(sortedIds.Count);
        foreach (var id in sortedIds)
        {
            if (!_wrapperTypes.TryGetValue(id, out var wrapperType))
            {
                throw new InvalidOperationException(
                    $"Wrapper type not found for function ID: {id}");
            }

            var wrapper = (IStatelessFunction)Activator.CreateInstance(wrapperType)!;
            wrapper.Initialize(container, facadeMap);
            result.Add(wrapper);
        }

        return result;
    }

    public IExecutionGraphBuilder CreateGraphBuilder() => new ExecutionGraphBuilder();
}
