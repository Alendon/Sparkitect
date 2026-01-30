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
    /// Gets a collection of all loaded mods with their file identifiers (ID + Version).
    /// </summary>
    IReadOnlyCollection<ModFileIdentifier> LoadedMods { get; }

    /// <summary>
    /// Gets loaded mods organized by loading groups. Each group represents a set of mods loaded together
    /// (e.g., engine mods first, then game mods).
    /// </summary>
    IReadOnlyList<IReadOnlyList<ModFileIdentifier>> LoadedModsPerGroup { get; }

    /// <summary>
    /// Gets all mod archives discovered during mod discovery.
    /// </summary>
    IReadOnlyList<ModManifest> DiscoveredArchives { get; }

    /// <summary>
    /// Discovers all available mods from the mods folder
    /// </summary>
    void DiscoverMods();

    /// <summary>
    /// Loads the specified mods by their file identifiers.
    /// </summary>
    /// <param name="identifiers">The mod file identifiers (ID + Version) to load.</param>
    void LoadMods(params ReadOnlySpan<ModFileIdentifier> identifiers);

    /// <summary>
    /// Unloads the last loaded mod group.
    /// </summary>
    /// <returns>The mod file identifiers that were unloaded.</returns>
    IReadOnlyList<ModFileIdentifier> UnloadLastModGroup();
}