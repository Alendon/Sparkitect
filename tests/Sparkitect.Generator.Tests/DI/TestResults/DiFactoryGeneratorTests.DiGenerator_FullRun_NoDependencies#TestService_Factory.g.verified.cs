//HintName: TestService_Factory.g.cs
namespace DiTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class TestService_Factory : Sparkitect.DI.IServiceFactory
{
    public Type ServiceType => typeof(global::DiTest.ITestService);
    public Type ImplementationType => typeof(TestService);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
    ];
    
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];
    
    public object CreateInstance(global::Sparkitect.DI.Container.ICoreContainerBuilder container)
    {
        

        return Constructor(

);
    
        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern TestService Constructor(

);
    }
    
    public void ApplyProperties(object instance, global::Sparkitect.DI.Container.ICoreContainerBuilder container)
    {
        if (instance is not TestService typedInstance)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to {nameof(TestService)}");

    
    
    
        
    }
}