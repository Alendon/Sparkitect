using System.Runtime.CompilerServices;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Utils;

namespace Sparkitect.DI.ServiceFactories;

[ServiceFactory<CliArgumentHandler>]
internal class CliArgumentHandlerFactory : IServiceFactory
{
    public Type ServiceType => typeof(ICliArgumentHandler);
    public Type ImplementationType => typeof(CliArgumentHandler);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];
    
    public object CreateInstance(ICoreContainerBuilder container)
    {
        return Constructor();
        
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        static extern CliArgumentHandler Constructor();
    }
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        if (instance is not CliArgumentHandler typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {typeof(CliArgumentHandler).Name}");
        
        // No properties to apply
    }
}