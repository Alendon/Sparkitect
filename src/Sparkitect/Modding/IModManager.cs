using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Sparkitect.DI;
using Sparkitect.DI.Container;

namespace Sparkitect.Modding;

/// <summary>
/// Interface for managing mods, including discovery, loading, and entrypoint resolution
/// </summary>
public interface IModManager
{
    //TODO adjust to expose a "hierarchy" of loaded mods. Eg first a group of engine mods get loaded and later a group of game mods
    
    ICoreContainer CurrentCoreContainer { get; }

    /// <summary>
    /// Gets a collection of all loaded mods
    /// </summary>
    IReadOnlyCollection<string> LoadedMods { get; }
    
    IReadOnlyList<IReadOnlyList<string>> LoadedModsPerGroup { get; }
    IReadOnlyList<ModManifest> DiscoveredArchives { get; }

    /// <summary>
    /// Discovers all available mods from the mods folder
    /// </summary>
    void DiscoverMods();

    /// <summary>
    /// Loads all discovered mods
    /// </summary>
    void LoadMods(params ReadOnlySpan<string> modIds);
        
    /// <summary>
    /// Creates an entrypoint container for the specified entrypoint type
    /// </summary>
    /// <typeparam name="T">The base type of entrypoints to include</typeparam>
    /// <param name="modsToInclude">The mods to include in the container (All or specific mod IDs)</param>
    /// <returns>A new entrypoint container with discovered entrypoints</returns>
    [MustDisposeResource] IEntrypointContainer<T> CreateEntrypointContainer<T>(
        OneOf<All, IEnumerable<string>> modsToInclude) where T : class, BaseConfigurationEntrypoint;
}