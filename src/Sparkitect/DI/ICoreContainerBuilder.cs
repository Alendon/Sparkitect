using System.Diagnostics.CodeAnalysis;

namespace Sparkitect.DI;

public interface ICoreContainerBuilder
{
    ICoreContainerBuilder Register(IServiceFactory serviceFactory);
    ICoreContainerBuilder Override(IServiceFactory serviceFactory);
    ICoreContainer Build();
    
    bool TryResolveInternal<T>([NotNullWhen(true)] out T? instance) where T : class;
}