using Sparkitect.DI.Resolution;

namespace Sparkitect.DI.Container;

/// <summary>
/// Builder for creating factory containers.
/// Scope is provided at Build time, not at construction.
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
internal class FactoryContainerBuilder<TBase> : IFactoryContainerBuilder<TBase>
    where TBase : class
{
    private readonly ICoreContainer _coreContainer;
    private readonly Dictionary<string, IKeyedFactory<TBase>> _factories = [];

    /// <summary>
    /// Creates a new factory container builder with the given core container
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    public FactoryContainerBuilder(ICoreContainer coreContainer)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
    }

    /// <summary>
    /// Registers a keyed factory with the builder
    /// </summary>
    /// <param name="keyedFactory">The keyed factory to register</param>
    /// <returns>The builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when a factory with the same key is already registered</exception>
    public IFactoryContainerBuilder<TBase> Register(IKeyedFactory<TBase> keyedFactory)
    {
        // Check for duplicate keys
        if (!_factories.TryAdd(keyedFactory.Key, keyedFactory))
            throw new InvalidOperationException($"A factory with key '{keyedFactory.Key}' is already registered");

        return this;
    }

    /// <summary>
    /// Returns the implementation types from each registered factory.
    /// These are the SG-generated wrapper/factory class types known after Register calls.
    /// </summary>
    public IReadOnlyList<Type> GetRegisteredWrapperTypes()
    {
        return _factories.Values.Select(f => f.GetType()).ToList();
    }

    /// <summary>
    /// Builds the factory container with all registered factories using the provided resolution scope
    /// </summary>
    /// <param name="scope">The resolution scope for dependency resolution during factory preparation</param>
    /// <param name="skipMissing">Skip factory entries which do not have all dependencies instead of throwing</param>
    /// <returns>The constructed factory container</returns>
    /// <exception cref="InvalidOperationException">Thrown when a factory's dependencies cannot be resolved</exception>
    public IFactoryContainer<TBase> Build(IResolutionScope scope, bool skipMissing = false)
    {
        var preparedFactories = new Dictionary<string, IKeyedFactory<TBase>>();

        foreach (var (key, factory) in _factories)
        {
            if (!factory.TryPrepare(scope))
            {
                if(skipMissing) continue;

                throw new InvalidOperationException(
                    $"Failed to prepare factory with key '{key}' of type '{factory.ImplementationType.Name}'. " +
                    "One or more required dependencies could not be resolved from the container.");
            }
            preparedFactories[key] = factory;
        }

        return new FactoryContainer<TBase>(preparedFactories);
    }

    /// <summary>
    /// Builds the factory container with a default empty resolution scope (fallback to container).
    /// </summary>
    public IFactoryContainer<TBase> Build(bool skipMissing = false)
    {
        var scope = new ResolutionScope(_coreContainer, null, new Dictionary<Type, Dictionary<Type, List<object>>>());
        return Build(scope, skipMissing);
    }
}
