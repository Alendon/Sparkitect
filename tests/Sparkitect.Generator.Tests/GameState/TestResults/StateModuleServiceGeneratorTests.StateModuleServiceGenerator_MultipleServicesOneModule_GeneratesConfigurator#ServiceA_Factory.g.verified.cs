//HintName: ServiceA_Factory.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace StateServiceTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class ServiceA_Factory : Sparkitect.DI.IServiceFactory
{
    public Type ServiceType => typeof(global::StateServiceTest.IServiceA);
    public Type ImplementationType => typeof(ServiceA);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
    ];

    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];

    public object CreateInstance(global::Sparkitect.DI.Container.ICoreContainerBuilder container, global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
    {
        

        return Constructor(

);

        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern ServiceA Constructor(

);
    }

    public object CreateInstance(global::Sparkitect.DI.Container.ICoreContainerBuilder container) => CreateInstance(container, new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Type>());

    public void ApplyProperties(object instance, global::Sparkitect.DI.Container.ICoreContainerBuilder container, global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
    {
        if (instance is not ServiceA typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {nameof(ServiceA)}");

    

    

    }

    public void ApplyProperties(object instance, global::Sparkitect.DI.Container.ICoreContainerBuilder container) => ApplyProperties(instance, container, new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Type>());
}
