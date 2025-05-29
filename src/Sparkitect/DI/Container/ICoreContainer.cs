using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Sparkitect.DI.Container;

[PublicAPI]
public interface ICoreContainer : IDisposable
{
    TService Resolve<TService>() where TService : class;
    bool TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class;
    bool TryResolve(Type serviceType, out object? service);
    
    IReadOnlyDictionary<Type, object> GetRegisteredInstances();
}