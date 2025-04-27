using System;
using System.Collections.Generic;
using System.ComponentModel;
using DryIoc;
using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Sparkitect.DI;
using IContainer = DryIoc.IContainer;

namespace Sparkitect.Modding;

/// <summary>
/// Interface for managing mods, including discovery, loading, and entrypoint resolution
/// </summary>
public interface IModManager
{
    //TODO adjust to expose a "hierarchy" of loaded mods. Eg first a group of engine mods get loaded and later a group of game mods
    
    IContainer CurrentCoreContainer { get; }

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

    [MustDisposeResource] IContainer CreateConfigurationContainer<T>(bool trackDisposeTransients,
        OneOf<All, IEnumerable<string>> modsToInclude) where T : BaseConfigurationEntrypoint;
    IContainer ModifyConfigurationContainer<T>(IContainer configurationContainer,
        OneOf<All, IEnumerable<string>> modsToInclude) where T : BaseConfigurationEntrypoint;
}