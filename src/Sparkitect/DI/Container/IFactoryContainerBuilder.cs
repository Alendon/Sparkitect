using JetBrains.Annotations;
using OneOf;
using Sparkitect.Modding;

namespace Sparkitect.DI.Container;

/// <summary>
/// Interface for builders that create factory containers
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
[PublicAPI]
public interface IFactoryContainerBuilder<TBase> where TBase : class
{
    /// <summary>
    /// Registers a keyed factory with the builder
    /// </summary>
    /// <param name="keyedFactory">The keyed factory to register</param>
    /// <returns>The builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory's key type doesn't match the builder's key type</exception>
    IFactoryContainerBuilder<TBase> Register(IKeyedFactory<TBase> keyedFactory);

    /// <summary>
    /// Builds the factory container with all registered factories
    /// </summary>
    /// <returns>The constructed factory container</returns>
    IFactoryContainer<TBase> Build();
}