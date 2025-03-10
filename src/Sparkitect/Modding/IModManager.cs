using System;
using System.Collections.Generic;
using DryIoc;
using JetBrains.Annotations;
using Sparkitect.DI;

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

    /// <summary>
    /// Discovers all available mods from the mods folder
    /// </summary>
    void DiscoverMods();

    /// <summary>
    /// Loads all discovered mods
    /// </summary>
    void LoadMods(params ReadOnlySpan<string> modIds);

    [MustDisposeResource] IContainer CreateConfigurationContainer<T>(bool trackDisposeTransients) where T : BaseConfigurationEntrypoint;
    IContainer CreateConfigurationContainer<T>(IContainer configurationContainer) where T : BaseConfigurationEntrypoint;
}