namespace Sparkitect.DI.Container;

/// <summary>
/// Builder for creating factory containers
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
internal class FactoryContainerBuilder<TBase> : IFactoryContainerBuilder<TBase>
    where TBase : class
{
    private readonly ICoreContainer _coreContainer;
    private readonly IReadOnlyDictionary<Type, Type>? _facadeMap;
    private readonly Dictionary<string, IKeyedFactory<TBase>> _factories = [];

    /// <summary>
    /// Creates a new factory container builder with the given core container
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    public FactoryContainerBuilder(ICoreContainer coreContainer)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
        _facadeMap = null;
    }

    /// <summary>
    /// Creates a new factory container builder with the given core container and facade map
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    /// <param name="facadeMap">Facade-to-service type mappings for dependency resolution</param>
    public FactoryContainerBuilder(ICoreContainer coreContainer, IReadOnlyDictionary<Type, Type> facadeMap)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
        _facadeMap = facadeMap;
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
    /// Builds the factory container with all registered factories
    /// </summary>
    /// <returns>The constructed factory container</returns>
    /// <exception cref="InvalidOperationException">Thrown when a factory's dependencies cannot be resolved</exception>
    public IFactoryContainer<TBase> Build(bool skipMissing = false)
    {
        var preparedFactories = new Dictionary<string, IKeyedFactory<TBase>>();

        foreach (var (key, factory) in _factories)
        {
            if (!factory.TryPrepare(_coreContainer, _facadeMap ?? new Dictionary<Type, Type>()))
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
}
