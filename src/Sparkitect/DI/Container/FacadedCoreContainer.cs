using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

internal class FacadedCoreContainer : IFacadedCoreContainer
{
    private readonly ICoreContainer _coreContainer;
    private readonly Dictionary<Type, Type> _facadeMap;
    private bool _disposed;
    
    
    public FacadedCoreContainer(ICoreContainer coreContainer, IReadOnlyDictionary<Type, Type> facadeMap)
    {
        _coreContainer = coreContainer;
        _facadeMap = new Dictionary<Type, Type>(facadeMap);
    }
    
    public TService Resolve<TService>() where TService : class
    {
       return _coreContainer.Resolve<TService>();
    }

    public bool TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        return _coreContainer.TryResolve(out service);   
    }

    public bool TryResolve(Type serviceType, out object? service)
    {
        return _coreContainer.TryResolve(serviceType, out service);  
    }

    public IReadOnlyDictionary<Type, object> GetCurrentRegisteredInstances()
    {
        return _coreContainer.GetCurrentRegisteredInstances();
    }

    public TFacade ResolveFacaded<TFacade>() where TFacade : class
    {
        if (!_facadeMap.TryGetValue(typeof(TFacade), out var serviceType))
            throw new DependencyResolutionException($"No facade found for service {typeof(TFacade).Name}");
        
        if (_disposed)
            throw new ObjectDisposedException(nameof(CoreContainer));
            
        if (!TryResolve(serviceType, out var instance))
            throw new DependencyResolutionException($"No registration found for service {serviceType.Name}");
        
        if(instance is not TFacade facaded)
            throw new DependencyResolutionException($"Facade {serviceType.Name} is not assignable to {typeof(TFacade).Name}");
            
        return facaded;
    }

    public bool TryResolveFacaded<TFacade>([NotNullWhen(true)] out TFacade? facade) where TFacade : class
    {
        var result = TryResolveFacaded(typeof(TFacade), out var resolved);
        facade = resolved as TFacade;
        return result && facade is not null;
    }

    public bool TryResolveFacaded(Type facadeType, out object? service)
    {
        if (!_facadeMap.TryGetValue(facadeType, out var serviceType))
            throw new DependencyResolutionException($"No facade found for service {facadeType}");
        
        return _coreContainer.TryResolve(serviceType, out service); 
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _facadeMap.Clear();
    }
}