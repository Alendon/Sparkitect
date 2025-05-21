using System.Runtime.CompilerServices;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.DI.ServiceFactories;

[ServiceFactory<IdentificationManager>]
internal class IdentificationManagerFactory : IServiceFactory
{
    public Type ServiceType => typeof(IIdentificationManager);
    public Type ImplementationType => typeof(IdentificationManager);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];
    
    public object CreateInstance(ICoreContainerBuilder container)
    {
        return Constructor();
        
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        static extern IdentificationManager Constructor();
    }
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        if (instance is not IdentificationManager typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {nameof(IdentificationManager)}");
            
        // No properties to apply
    }
}