//HintName: TypedProviderRegistry_KeyedFactory.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace DiTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class TypedProviderRegistry_KeyedFactory : global::Sparkitect.DI.IKeyedFactory<Sparkitect.Modding.IRegistryBase>
{
    // Cached dependencies
    
    private DiTest.IValueManager _arg_1;
    
    

    public Type ImplementationType => typeof(TypedProviderRegistry);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
        (typeof(DiTest.IValueManager), false) 
    ];

    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];

    public bool TryPrepare(global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        bool allResolved = true;

        
            if(!scope.TryResolve<DiTest.IValueManager>(typeof(TypedProviderRegistry_KeyedFactory), out _arg_1))
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

    public Sparkitect.Modding.IRegistryBase CreateInstance()
    {
        // Validate that all required dependencies are prepared
    
        
        if (_arg_1 is null)
            throw global::Sparkitect.DI.Exceptions.DependencyResolutionException.CreateForConstructor<TypedProviderRegistry, DiTest.IValueManager>("");
        
    
    

        var instance = Constructor(

    _arg_1  
);

        // Apply cached property dependencies
    

        return instance;

        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern TypedProviderRegistry Constructor(

    DiTest.IValueManager arg_1  
);

    
    }
}
