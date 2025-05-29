using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Sparkitect.DI.Container;

[PublicAPI]
public interface ICoreContainerBuilder
{
    ICoreContainerBuilder Register<TServiceFactory>() where TServiceFactory : IServiceFactory, new();
    ICoreContainerBuilder Override<TServiceFactory>() where TServiceFactory : IServiceFactory, new();
    ICoreContainer Build();
    
    bool TryResolveInternal<T>([NotNullWhen(true)] out T? instance) where T : class;
}