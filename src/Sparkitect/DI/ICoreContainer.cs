namespace Sparkitect.DI;

public interface ICoreContainer : IDisposable
{
    TService Resolve<TService>() where TService : class;
    bool TryResolve<TService>(out TService? service) where TService : class;
    bool TryResolve(Type serviceType, out object? service);
    
    IReadOnlyDictionary<Type, object> GetRegisteredInstances();
}