//HintName: TypedRegistry_KeyedFactory.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace DiTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class TypedRegistry_KeyedFactory : global::Sparkitect.DI.IKeyedFactory<Sparkitect.Modding.IRegistryBase>
{
    // Cached dependencies
    
    

    public Type ImplementationType => typeof(TypedRegistry);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [
    
    ];

    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [
    
    ];

    public bool TryPrepare(global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        bool allResolved = true;

        

        

        if (!allResolved)
        {
            // Clear all dependency fields on failure
        
        
            return false;
        }

        return true;
    }

    public Sparkitect.Modding.IRegistryBase CreateInstance()
    {
        // Validate that all required dependencies are prepared
    
    

        var instance = Constructor(

);

        // Apply cached property dependencies
    

        return instance;

        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]
        static extern TypedRegistry Constructor(

);

    
    }
}
