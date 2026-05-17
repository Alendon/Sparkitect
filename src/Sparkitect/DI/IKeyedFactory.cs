using JetBrains.Annotations;
using Sparkitect.DI.Resolution;

namespace Sparkitect.DI;

/// <summary>
/// Interface for factories that create instances with dependencies.
/// The key associated with this factory is owned by the configurator that registers it,
/// not by the factory itself.
/// </summary>
/// <typeparam name="TBase">The base type of the object to create</typeparam>
[PublicAPI]
public interface IKeyedFactory<TBase> : IFactoryBase where TBase : class
{
    /// <summary>
    /// Attempts to prepare the factory by resolving and caching all dependencies
    /// </summary>
    /// <param name="scope">The resolution scope to resolve dependencies from</param>
    /// <returns>True if all required dependencies were resolved; false otherwise. On failure, all dependency fields are cleared.</returns>
    bool TryPrepare(IResolutionScope scope);

    /// <summary>
    /// Creates a new instance of the object using cached dependencies
    /// </summary>
    /// <returns>The created instance</returns>
    TBase CreateInstance();
}
