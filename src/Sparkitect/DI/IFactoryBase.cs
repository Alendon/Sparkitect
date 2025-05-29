namespace Sparkitect.DI;

/// <summary>
/// Base interface for all factory implementations that create instances with dependencies
/// </summary>
public interface IFactoryBase
{
    /// <summary>
    /// Gets the implementation type that this factory creates
    /// </summary>
    Type ImplementationType { get; }
    
    /// <summary>
    /// Gets the required and optional constructor dependencies for the implementation type
    /// </summary>
    /// <returns>An array of tuples containing the dependency type and whether it's optional</returns>
    (Type Type, bool IsOptional)[] GetConstructorDependencies();
    
    /// <summary>
    /// Gets the required and optional property dependencies for the implementation type
    /// </summary>
    /// <returns>An array of tuples containing the dependency type, property name, and whether it's optional</returns>
    (Type Type, bool IsOptional)[] GetPropertyDependencies();
}