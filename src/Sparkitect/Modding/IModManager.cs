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
    
    

    /// <summary>
    /// Gets a collection of all loaded mods
    /// </summary>
    IReadOnlyCollection<string> LoadedMods { get; }

    /// <summary>
    /// Gets loaded mods organized by loading groups. Each group represents a set of mods loaded together
    /// (e.g., engine mods first, then game mods).
    /// </summary>
    IReadOnlyList<IReadOnlyList<string>> LoadedModsPerGroup { get; }

    /// <summary>
    /// Gets all mod archives discovered during mod discovery.
    /// </summary>
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
    /// Unloads the last loaded mod group
    /// </summary>
    /// <returns>The mod IDs that were unloaded</returns>
    IReadOnlyList<string> UnloadLastModGroup();
}