//HintName: JsonProcessor_KeyedFactory.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

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
    
    public bool TryPrepare(global::Sparkitect.DI.Container.ICoreContainer container, global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
    {
        bool allResolved = true;

        
            if(!container.TryResolveMapped<global::DiTest.ILogger>(out _arg_1, facadeMap))
            {
            
                allResolved = false;
            
            }
        

        

        if (!allResolved)
        {
            // Clear all dependency fields on failure
        
            _arg_1 = default;
        
        
            return false;
        }

        return true;
    }

    public bool TryPrepare(global::Sparkitect.DI.Container.ICoreContainer container) => TryPrepare(container, new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Type>());
    
    public global::DiTest.IProcessor CreateInstance()
    {
        // Validate that all required dependencies are prepared
    
        
        if (_arg_1 is null)
            throw global::Sparkitect.DI.Exceptions.DependencyResolutionException.Create<JsonProcessor, global::DiTest.ILogger>();
        
    
    

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