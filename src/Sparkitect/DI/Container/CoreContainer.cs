using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

internal sealed class CoreContainer : ICoreContainer
{
    private readonly Dictionary<Type, object> _instances;
    private bool _disposed;
    
    public CoreContainer(Dictionary<Type, object> instances)
    {
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _disposed = false;
    }
    
    public TService Resolve<TService>() where TService : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CoreContainer));
            
        var serviceType = typeof(TService);
        var instance = Resolve(serviceType);
        
        if (instance is not TService typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {typeof(TService).Name}");
        
        return typedInstance;
    }
    
    public object Resolve(Type serviceType)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CoreContainer));
            
        if (serviceType is null)
            throw new ArgumentNullException(nameof(serviceType));
            
        if (!_instances.TryGetValue(serviceType, out var instance))
            throw new DependencyResolutionException($"No registration found for service {serviceType.Name}");
            
        return instance;
    }
    
    public bool TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (_disposed)
        {
            service = null;
            return false;
        }
            
        var result = TryResolve(typeof(TService), out var instance);
        service = instance as TService;
        return result && service is not null;
    }
    
    public bool TryResolve(Type serviceType, [NotNullWhen(true)] out object? service)
    {
        if (_disposed || serviceType is null)
        {
            service = null;
            return false;
        }
        
        return _instances.TryGetValue(serviceType, out service);
    }
    
    public IReadOnlyDictionary<Type, object> GetRegisteredInstances()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CoreContainer));
            
        return _instances;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        // Dispose all disposable services
        foreach (var instance in _instances.Values)
        {
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception)
                {
                    // Suppress exceptions during disposal
                }
            }
        }
        
        _instances.Clear();
        _disposed = true;
    }
}