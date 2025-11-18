namespace Sparkitect.Modding;

/// <summary>
/// Manages registry processing and tracks which mods are registered per registry
/// </summary>
public interface IRegistryManager
{
    /// <summary>
    /// Process a specific registry for the given mods
    /// </summary>
    void ProcessRegistry<TRegistry>(params Span<ushort> modIds) where TRegistry : class, IRegistry;
    
    /// <summary>
    /// Process all currently loaded mods that have not yet been processed for the given registry
    /// </summary>
    void ProcessAllMissing<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Unregister all mods currently processed for the given registry
    /// </summary>
    void UnregisterAllRemaining<TRegistry>() where TRegistry : class, IRegistry;

    void AddRegistry<TRegistry>() where TRegistry : class, IRegistry;
}