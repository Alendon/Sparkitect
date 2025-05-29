using JetBrains.Annotations;

namespace Sparkitect.DI.Container;

/// <summary>
/// Interface for builders that create entrypoint containers
/// </summary>
/// <typeparam name="TBase">The base type for the entrypoint</typeparam>
[PublicAPI]
public interface IEntrypointContainerBuilder<TBase> where TBase : class
{
    /// <summary>
    /// Registers an entrypoint factory with the builder
    /// </summary>
    /// <param name="entrypointFactory">The entrypoint factory to register</param>
    /// <returns>The builder instance for method chaining</returns>
    IEntrypointContainerBuilder<TBase> Register(IEntrypointFactory<TBase> entrypointFactory);

    /// <summary>
    /// Builds the entrypoint container with all registered entrypoints
    /// </summary>
    /// <returns>The constructed entrypoint container</returns>
    IEntrypointContainer<TBase> Build();
}