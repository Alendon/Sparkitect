//HintName: CoreService_Factory.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace StateServiceTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class CoreService_Factory : Sparkitect.DI.IServiceFactory
{
    public Type ServiceType => typeof(global::StateServiceTest.ICoreService);
    public Type ImplementationType => typeof(CoreService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
    ];

    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];

    public object CreateInstance(global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        

        return Constructor(

);

        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern CoreService Constructor(

);
    }

    public void ApplyProperties(object instance, global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        if (instance is not CoreService typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {nameof(CoreService)}");

    

    

    }
}
