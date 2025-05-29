using OneOf;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Interface for factories that create keyed instances with dependencies
/// </summary>
/// <typeparam name="TBase">The base type of the object to create</typeparam>
public interface IKeyedFactory<TBase> : IFactoryBase where TBase : class
{
    /// <summary>
    /// The key used to identify this factory
    /// </summary>
    OneOf<Identification, string> Key { get; }
    
    /// <summary>
    /// Prepares the factory by resolving and caching all dependencies
    /// </summary>
    /// <param name="container">The container to resolve dependencies from</param>
    void Prepare(ICoreContainer container);
    
    /// <summary>
    /// Creates a new instance of the object using cached dependencies
    /// </summary>
    /// <returns>The created instance</returns>
    TBase CreateInstance();
}