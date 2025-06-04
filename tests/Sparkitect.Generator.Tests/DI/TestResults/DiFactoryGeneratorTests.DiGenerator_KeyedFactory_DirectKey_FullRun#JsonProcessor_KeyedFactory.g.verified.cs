//HintName: JsonProcessor_KeyedFactory.g.cs
namespace DiTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class JsonProcessor_KeyedFactory : global::Sparkitect.DI.IKeyedFactory<global::DiTest.IProcessor>
{
    // Cached dependencies
    
    private global::DiTest.ILogger _arg_1;
    
    
    
    public Type ImplementationType => typeof(JsonProcessor);
    
    
    
    public global::OneOf.OneOf<global::Sparkitect.Modding.Identification, string> Key => "json";
    
    
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
        (typeof(global::DiTest.ILogger), false) 
    ];
    
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];
    
    public void Prepare(global::Sparkitect.DI.Container.ICoreContainer container)
    {
        // Resolve and cache constructor dependencies
        
            if(!container.TryResolve<global::DiTest.ILogger>(out _arg_1))
            {
            
                throw global::Sparkitect.DI.Exceptions.DependencyResolutionException.Create<JsonProcessor, global::DiTest.ILogger>();
            
            }
        
        
        // Resolve and cache property dependencies
        
    }
    
    public global::DiTest.IProcessor CreateInstance()
    {
        var instance = Constructor(

    _arg_1  
);
        
        // Apply cached property dependencies
    
        
        return instance;
    
        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern JsonProcessor Constructor(

    global::DiTest.ILogger arg_1  
);
        
    
    }
}