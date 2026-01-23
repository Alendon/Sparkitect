using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public interface IStatelessFunctionManager
{
    IReadOnlyList<IStatelessFunction> GetSorted<TStatelessFunction, TContext, TRegistry>(
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap,
        TContext context)
        where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry;

    IExecutionGraphBuilder CreateGraphBuilder();
}
