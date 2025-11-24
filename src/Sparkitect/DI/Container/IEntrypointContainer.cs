using JetBrains.Annotations;

namespace Sparkitect.DI.Container;

/// <summary>
/// Container for entrypoint classes that share a common base type.
/// Allows retrieving all registered implementations of the specified base type.
/// </summary>
/// <typeparam name="TBase">The base type for entrypoint classes</typeparam>
[PublicAPI]
public interface IEntrypointContainer<out TBase> : IDisposable where TBase : class
{
    /// <summary>
    /// Resolves all implementations of the base type from the container
    /// </summary>
    /// <returns>A read-only list of all registered implementations</returns>
    IReadOnlyList<TBase> ResolveMany();

    /// <summary>
    /// Executes an action on all entrypoint implementations.
    /// </summary>
    /// <param name="action">The action to execute for each implementation.</param>
    void ProcessMany(Action<TBase> action);
}