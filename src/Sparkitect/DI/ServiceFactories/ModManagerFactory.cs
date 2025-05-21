using System.Runtime.CompilerServices;
using Sparkitect.DI.Exceptions;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.DI.ServiceFactories;

[ServiceFactory<ModManager>]
internal class ModManagerFactory : IServiceFactory
{
    public Type ServiceType => typeof(IModManager);
    public Type ImplementationType => typeof(ModManager);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => 
    [
        (typeof(ICliArgumentHandler), false),
        (typeof(IIdentificationManager), false)
    ];
    
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];
    
    public object CreateInstance(ICoreContainerBuilder container)
    {
        // This implementation will be replaced by IL weaving to call __CreateInstance
        if (!container.TryResolveInternal<ICliArgumentHandler>(out var cliArgumentHandler) ||
            !container.TryResolveInternal<IIdentificationManager>(out var identificationManager))
            throw new DependencyResolutionException("Failed to resolve required dependencies for ModManager");
        
        
        
        return Constructor(cliArgumentHandler, identificationManager);
        
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        static extern ModManager Constructor(
            ICliArgumentHandler cliArgumentHandler,
            IIdentificationManager identificationManager);
    }
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        if (instance is not ModManager)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {nameof(ModManager)}");
            
        //Nothing to apply here
    }
    

}