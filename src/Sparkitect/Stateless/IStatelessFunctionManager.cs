using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

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
    
    /// <summary>
    /// Registers a stateless function with its identification.
    /// </summary>
    /// <typeparam name="TStatelessFunction">The generated wrapper type implementing IStatelessFunction.</typeparam>
    /// <param name="id">The function's identification.</param>
    internal void AddFunction<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction;
}
