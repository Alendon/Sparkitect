using JetBrains.Annotations;
using Sparkitect.DI.Resolution;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Tracks registered stateless-function wrapper types and instantiates them against a resolution scope.
/// One instance is registered per core container as a state service.
/// </summary>
[PublicAPI]
public interface IStatelessFunctionManager
{
    /// <summary>
    /// Instantiates stateless function wrappers for the given sorted IDs.
    /// Each ID must have been previously registered via AddFunction.
    /// Wrappers are created via Activator.CreateInstance and initialized with the resolution scope.
    /// </summary>
    /// <param name="sortedIds">The topologically sorted function IDs.</param>
    /// <param name="scope">The resolution scope for wrapper initialization.</param>
    /// <returns>The instantiated and initialized stateless function wrappers in sorted order.</returns>
    IReadOnlyList<IStatelessFunction> InstantiateWrappers(
        IReadOnlyList<Identification> sortedIds,
        IResolutionScope scope);

    /// <summary>Creates a fresh <see cref="IExecutionGraphBuilder"/> for ordering a single execution pass.</summary>
    IExecutionGraphBuilder CreateGraphBuilder();

    /// <summary>
    /// Gets the CLR types of all registered stateless function wrappers.
    /// Used by callers to build resolution scopes with the correct wrapper type set.
    /// </summary>
    IReadOnlyCollection<Type> GetRegisteredWrapperTypes();

    /// <summary>
    /// Registers a stateless function with its identification.
    /// </summary>
    /// <typeparam name="TStatelessFunction">The generated wrapper type implementing IStatelessFunction.</typeparam>
    /// <param name="id">The function's identification.</param>
    internal void AddFunction<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction;

    /// <summary>
    /// Removes a previously registered stateless function. Must run when the owning registration is
    /// reversed: a retained wrapper <see cref="Type"/> from an unloaded mod pins its whole load context.
    /// </summary>
    /// <param name="id">The function's identification.</param>
    internal void RemoveFunction(Identification id);
}
