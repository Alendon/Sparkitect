using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.GameState;

[PublicAPI]
public interface IStateContainer : ICoreContainer
{
    // Reserved for future facade-aware resolution if needed by wrappers
    
    TService ResolveFacaded<TService>() where TService : class;
    
    bool TryResolveFacaded<TService>([NotNullWhen(true)] out TService? service) where TService : class;
    
    bool TryResolveFacaded(Type serviceType, out object? service);
}

[PublicAPI]
public interface IStateContainerBuilder : ICoreContainerBuilder
{
    IStateContainerBuilder Register<TServiceFactory, TFacade>() where TServiceFactory : IServiceFactory, new() where TFacade : class;
    IStateContainer BuildStateContainer();
}

internal sealed class StateContainer : IStateContainer
{
    private readonly ICoreContainer _inner;
    private readonly Dictionary<Type, Type> _facadeMapping;


    public StateContainer(ICoreContainer inner, Dictionary<Type, Type> facadeMapping)
    {
        _inner = inner;
        _facadeMapping = facadeMapping;
    }


    public void Dispose()
    {
        _inner.Dispose();
    }

    TService ICoreContainer.Resolve<TService>()
    {
       return _inner.Resolve<TService>();
    }

    bool ICoreContainer.TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        return _inner.TryResolve(out service);
    }

    bool ICoreContainer.TryResolve(Type serviceType, out object? service)
    {
        return _inner.TryResolve(serviceType, out service);
    }

    IReadOnlyDictionary<Type, object> ICoreContainer.GetCurrentRegisteredInstances()
    {
        return _inner.GetCurrentRegisteredInstances();
    }

    public TService ResolveFacaded<TService>() where TService : class
    {
        if (TryResolveFacaded(out TService? service))
        {
            return service;
        }

        throw new DependencyResolutionException($"No registration found for service {typeof(TService).FullName}");
    }

    public bool TryResolveFacaded<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        var success = TryResolveFacaded(typeof(TService), out var result);
        service = result as TService;

        return success && service is not null;
    }

    public bool TryResolveFacaded(Type serviceType, out object? service)
    {
        _facadeMapping.TryGetValue(serviceType, out var queryType);
        queryType ??= serviceType;
        return _inner.TryResolve(queryType, out service);
    }
}

internal sealed class StateContainerBuilder : CoreContainerBuilder, IStateContainerBuilder
{
    private readonly Dictionary<Type, Type> FacadeMapping = new();

    public StateContainerBuilder(ICoreContainer? parentContainer) : base(parentContainer)
    {
    }

    public IStateContainerBuilder Register<TServiceFactory, TFacade>() where TServiceFactory : IServiceFactory, new() where TFacade : class
    {
        var factory = new TServiceFactory();

        if (!factory.ImplementationType.IsAssignableTo(typeof(TFacade)))
        {
            throw new ArgumentException($"Provided Facade {typeof(TFacade)} is not compatible with  {typeof(TServiceFactory)}");
        }

        Register<TServiceFactory>();
        FacadeMapping[typeof(TFacade)] = factory.ServiceType;

        return this;
    }

    public IStateContainer BuildStateContainer()
    {

        var inner = Build();
        
        
        return new StateContainer(inner, FacadeMapping);
    }
    
}

[PublicAPI]
public interface IModuleScope
{
    bool TryResolveExposed<T>([NotNullWhen(true)] out T? service) where T : class;
    bool TryResolveFacade<T>([NotNullWhen(true)] out T? facade) where T : class;
}

