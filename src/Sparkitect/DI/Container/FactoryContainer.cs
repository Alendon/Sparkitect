using System.Diagnostics.CodeAnalysis;
using OneOf;
using Sparkitect.Modding;
using Serilog;

namespace Sparkitect.DI.Container;

/// <summary>
/// Implementation of a factory container that stores and provides access to keyed factories
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
internal sealed class FactoryContainer<TBase> : IFactoryContainer<TBase> 
    where TBase : class
{
    private readonly Dictionary<OneOf<Identification, string>, IKeyedFactory<TBase>> _factories;
    private bool _disposed;
    public IReadOnlyDictionary<OneOf<Identification, string>, Type> Metadata { get; }
    
    
    /// <summary>
    /// Creates a new factory container with the given prepared factories
    /// </summary>
    /// <param name="factories">The dictionary of prepared keyed factories</param>
    public FactoryContainer(Dictionary<OneOf<Identification, string>, IKeyedFactory<TBase>> factories)
    {
        _factories = factories ?? throw new ArgumentNullException(nameof(factories));
        _disposed = false;

        Metadata = _factories.ToDictionary(x => x.Key, x => x.Value.ImplementationType);
    }
    
    /// <summary>
    /// Resolves all factories and creates new instances for each
    /// </summary>
    /// <returns>A read-only dictionary of all factory keys and their created instances</returns>
    public IReadOnlyDictionary<OneOf<Identification, string>, TBase> ResolveAll()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FactoryContainer<TBase>));
            
        var result = new Dictionary<OneOf<Identification, string>, TBase>();
        
        foreach (var (key, factory) in _factories)
        {
            var instance = factory.CreateInstance();
            result[key] = instance;
        }
        
        return result;
    }
    
    /// <summary>
    /// Attempts to resolve a factory by key and create an instance
    /// </summary>
    /// <param name="key">The key to look up the factory</param>
    /// <param name="instance">The created instance if found</param>
    /// <returns>True if the factory was found and instance created successfully</returns>
    public bool TryResolve(OneOf<Identification, string> key, [NotNullWhen(true)] out TBase? instance)
    {
        if (_disposed)
        {
            instance = null;
            return false;
        }
        
        if (_factories.TryGetValue(key, out var factory))
        {
            instance = factory.CreateInstance();
            return true;
        }
        
        instance = null;
        return false;
    }


    /// <summary>
    /// Disposes the container and all disposable factory instances
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        // Dispose all disposable factories
        foreach (var factory in _factories.Values)
        {
            if (factory is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    // Log disposal exceptions but don't let them propagate to prevent cascading failures
                    Log.Warning(ex, "Exception occurred while disposing factory of type {FactoryType}", factory.GetType().Name);
                }
            }
        }
        
        _factories.Clear();
        _disposed = true;
    }
}