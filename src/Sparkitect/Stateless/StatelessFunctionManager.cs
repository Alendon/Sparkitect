using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

[CreateServiceFactory<IStatelessFunctionManager>]
internal sealed class StatelessFunctionManager : IStatelessFunctionManager
{
    public required IModDIService ModDIService { get; init; }

    public IReadOnlyList<IStatelessFunction> GetSorted<TStatelessFunction, TContext, TRegistry>(
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap,
        TContext context)
        where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry
    {
        // STUB: Returns empty for now
        // TODO: Use ApplySchedulingEntrypoint<TStatelessFunction> to collect functions
        // TODO: Each scheduler decides inclusion based on context
        // TODO: Build graph, topological sort
        // TODO: Instantiate wrappers, call Initialize(container, facadeMap)
        return [];
    }

    public IExecutionGraphBuilder CreateGraphBuilder() => new ExecutionGraphBuilder();
}
