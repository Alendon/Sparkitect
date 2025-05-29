//HintName: TestEntrypoint_EntrypointFactory.g.cs
namespace DiTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.GeneratorAttributes.EntrypointFactoryAttribute<global::DiTest.IMyEntrypoint>]
internal class TestEntrypoint_EntrypointFactory : global::Sparkitect.DI.IEntrypointFactory<global::DiTest.IMyEntrypoint>
{
    public Type ImplementationType => typeof(TestEntrypoint);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
        (typeof(global::DiTest.IService1), false) ,
        (typeof(global::DiTest.IService2), true) 
    ];
    
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
        (typeof(global::DiTest.IService3), false) 
    ];
    
    public global::DiTest.IMyEntrypoint CreateInstance(global::Sparkitect.DI.Container.ICoreContainer container)
    {
        
            if(!container.TryResolve<global::DiTest.IService1>(out var arg_1))
            {
            
                throw global::Sparkitect.DI.Exceptions.DependencyResolutionException.Create<TestEntrypoint, global::DiTest.IService1>();
            
            }
        
            if(!container.TryResolve<global::DiTest.IService2>(out var arg_2))
            {
            
            }
        

        var instance = Constructor(

    arg_1  ,
    arg_2  
);
        
        // Apply property dependencies
    
        if(!container.TryResolve<global::DiTest.IService3>(out var prop_1))
        {
        
            throw global::Sparkitect.DI.Exceptions.DependencyResolutionException.Create<TestEntrypoint, global::DiTest.IService3>();
        
        }
        else
        {
            SetProperty_1(instance, prop_1);
        }
    
        
        return instance;
    
        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern TestEntrypoint Constructor(

    global::DiTest.IService1 arg_1  ,
    global::DiTest.IService2 arg_2  
);
        
    
        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_Service3")]
        static extern void SetProperty_1(TestEntrypoint target, global::DiTest.IService3 value);
    
    }
}