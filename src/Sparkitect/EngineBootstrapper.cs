using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DryIoc;
using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect;

/// <summary>
/// Manages the engine initialization process, from application startup to the transition to the first game state.
/// </summary>
public class EngineBootstrapper
{
    private IContainer? _coreContainer;
    private IContainer? _rootContainer;
    private IModManager? _modManager;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    public static void Main()
    {
        var bootstrapper = new EngineBootstrapper();

        try
        {
            bootstrapper.BuildCoreContainer();
            bootstrapper.LoadRootMods();
            bootstrapper.ProcessRegistries();
            bootstrapper.RunGame();
        }
        finally
        {
            bootstrapper.CleanUp();
        }
    }

    /// <summary>
    /// Builds the core container with essential services needed for engine initialization
    /// </summary>
    public void BuildCoreContainer()
    {
        _coreContainer = new Container();

        // Register essential services
        _coreContainer.Register<IModManager, ModManager>(Reuse.Singleton);

        // Resolve the mod manager for later use
        _modManager = _coreContainer.Resolve<IModManager>();
    }

    /// <summary>
    /// Discovers and loads all mods from the mods folder
    /// </summary>
    public void LoadRootMods()
    {
        if (_modManager is null)
        {
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        // Discover and load all mods
        _modManager.DiscoverMods();
        _modManager.LoadMods();

        // Build the root container with services from all loaded mods
        BuildRootContainer();
    }

    /// <summary>
    /// Builds the root container with services from all loaded mods
    /// </summary>
    private void BuildRootContainer()
    {
        if (_modManager is null)
        {
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        // Create a new container based on the core container
        _rootContainer = _coreContainer.CreateChild();

        // Discover and execute all IoC builder entrypoints
        using var iocBuilderEntrypoints = _modManager.CreateConfigurationContainer<CoreConfigurator>(true);
        foreach (var entrypoint in iocBuilderEntrypoints.ResolveMany<CoreConfigurator>())
        {
            entrypoint.ConfigureIoc(_rootContainer);
        }
    }

    /// <summary>
    /// Processes all registries from loaded mods
    /// </summary>
    public void ProcessRegistries()
    {
        if (_modManager is null)
        {
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        // Create a registry container
        using var registryContainer = _modManager.CreateConfigurationContainer<IIoCRegistryBuilder>(true);
        
         
        // Discover and execute all registry builder entrypoints
        foreach (var entrypoint in registryContainer.ResolveMany<IIoCRegistryBuilder>())
        {
            entrypoint.ConfigureRegistries(registryContainer);
        }
        
        _modManager.CreateConfigurationContainer<IRegistrations>(registryContainer);

        // Discover and execute all registration entrypoints
        foreach (var entrypoint in registryContainer.ResolveMany<IRegistrations>())
        {
            // Execute the main phase registration
            entrypoint.MainPhaseRegistration();
        }
    }

    /// <summary>
    /// Transitions to the first game state and runs the game loop
    /// </summary>
    public void RunGame()
    {
        // Currently a placeholder until the game state system is implemented
        Console.WriteLine("Game is running...");

        // In a real implementation, this would initialize the game state system
        // and transition to the first game state
    }

    /// <summary>
    /// Cleans up resources when the engine shuts down
    /// </summary>
    public void CleanUp()
    {
        // Dispose containers in reverse order of creation
        _rootContainer?.Dispose();
        _coreContainer?.Dispose();
    }
}