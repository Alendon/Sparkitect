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

    /// <summary>
    /// Instantiates stateless function wrappers for the given sorted IDs.
    /// Each ID must have been previously registered via AddFunction.
    /// Wrappers are created via Activator.CreateInstance and initialized with the container and facade map.
    /// </summary>
    /// <param name="sortedIds">The topologically sorted function IDs.</param>
    /// <param name="container">The core container for wrapper initialization.</param>
    /// <param name="facadeMap">The facade type map for wrapper initialization.</param>
    /// <returns>The instantiated and initialized stateless function wrappers in sorted order.</returns>
    IReadOnlyList<IStatelessFunction> InstantiateWrappers(
        IReadOnlyList<Identification> sortedIds,
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap);

    IExecutionGraphBuilder CreateGraphBuilder();

    /// <summary>
    /// Registers a stateless function with its identification.
    /// </summary>
    /// <typeparam name="TStatelessFunction">The generated wrapper type implementing IStatelessFunction.</typeparam>
    /// <param name="id">The function's identification.</param>
    internal void AddFunction<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction;
}
