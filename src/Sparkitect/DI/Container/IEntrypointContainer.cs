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
    
    void ProcessMany(Action<TBase> action);
}