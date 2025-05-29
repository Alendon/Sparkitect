using Sparkitect.DI.Container;

namespace Sparkitect.DI;

/// <summary>
/// Interface for factories that create entrypoint instances with dependencies
/// </summary>
/// <typeparam name="TBase">The base type of the entrypoint</typeparam>
public interface IEntrypointFactory<TBase> : IFactoryBase where TBase : class
{
    /// <summary>
    /// Creates an instance of the entrypoint using the provided container for resolving dependencies
    /// </summary>
    /// <param name="container">The container to resolve dependencies from</param>
    /// <returns>The created entrypoint instance</returns>
    TBase CreateInstance(ICoreContainer container);
}