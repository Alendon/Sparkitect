using Sparkitect.DI.Container;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

[RegistryFacade<IStatelessFunctionRegistrar>]
public interface IStatelessFunctionManager
{
    IReadOnlyList<IStatelessFunction> GetSorted<TStatelessFunction, TContext, TRegistry>(
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap,
        TContext context,
        IEnumerable<string> loadedMods)
        where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry;

    IExecutionGraphBuilder CreateGraphBuilder();
}
