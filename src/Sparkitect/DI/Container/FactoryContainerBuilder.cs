using OneOf;
using Sparkitect.Modding;

namespace Sparkitect.DI.Container;

/// <summary>
/// Specifies the allowed key type for factory container keys.
/// </summary>
public enum FactoryKeyType
{
    /// <summary>
    /// Factory keys are strings.
    /// </summary>
    String,

    /// <summary>
    /// Factory keys are <see cref="Sparkitect.Modding.Identification"/> structs.
    /// </summary>
    Identification
}

/// <summary>
/// Builder for creating factory containers
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
internal class FactoryContainerBuilder<TBase> : IFactoryContainerBuilder<TBase>
    where TBase : class
{
    private readonly ICoreContainer _coreContainer;
    private readonly FactoryKeyType _allowedKeyType;
    private readonly IReadOnlyDictionary<Type, Type>? _facadeMap;
    private readonly Dictionary<OneOf<Identification, string>, IKeyedFactory<TBase>> _factories = [];

    /// <summary>
    /// Creates a new factory container builder with the given core container and key type constraint
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    /// <param name="allowedKeyType">The key type that this builder accepts (locks the key type)</param>
    public FactoryContainerBuilder(ICoreContainer coreContainer, FactoryKeyType allowedKeyType)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
        _allowedKeyType = allowedKeyType;
        _facadeMap = null;
    }

    /// <summary>
    /// Creates a new factory container builder with the given core container, key type constraint, and facade map
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    /// <param name="allowedKeyType">The key type that this builder accepts (locks the key type)</param>
    /// <param name="facadeMap">Facade-to-service type mappings for dependency resolution</param>
    public FactoryContainerBuilder(ICoreContainer coreContainer, FactoryKeyType allowedKeyType, IReadOnlyDictionary<Type, Type> facadeMap)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
        _allowedKeyType = allowedKeyType;
        _facadeMap = facadeMap;
    }
    
    /// <summary>
    /// Registers a keyed factory with the builder
    /// </summary>
    /// <param name="keyedFactory">The keyed factory to register</param>
    /// <returns>The builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory's key type doesn't match the builder's key type</exception>
    public IFactoryContainerBuilder<TBase> Register(IKeyedFactory<TBase> keyedFactory)
    {
        // Validate key type matches the builder's allowed key type
        ValidateKeyType(keyedFactory.Key);
        
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
        var preparedFactories = new Dictionary<OneOf<Identification, string>, IKeyedFactory<TBase>>();

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
    
    /// <summary>
    /// Validates that the factory's key type matches the builder's allowed key type
    /// </summary>
    /// <param name="factoryKey">The factory's key to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when key types don't match</exception>
    private void ValidateKeyType(OneOf<Identification, string> factoryKey)
    {
        var factoryIsIdentification = factoryKey.IsT0;
        var expectedIsIdentification = _allowedKeyType == FactoryKeyType.Identification;
        
        if (factoryIsIdentification != expectedIsIdentification)
        {
            var expectedType = expectedIsIdentification ? "Identification" : "string";
            var factoryType = factoryIsIdentification ? "Identification" : "string";
            throw new InvalidOperationException(
                $"Key type mismatch: Builder accepts {expectedType} keys, but factory provides {factoryType} key");
        }
    }
}