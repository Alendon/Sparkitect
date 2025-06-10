using OneOf;
using Sparkitect.Modding;

namespace Sparkitect.DI.Container;

/// <summary>
/// Enum to specify the allowed key type for a factory container builder
/// </summary>
public enum FactoryKeyType
{
    String,
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
    public IFactoryContainer<TBase> Build()
    {
        var preparedFactories = new Dictionary<OneOf<Identification, string>, IKeyedFactory<TBase>>();
        
        // Prepare all factories
        foreach (var (key, factory) in _factories)
        {
            factory.Prepare(_coreContainer);
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