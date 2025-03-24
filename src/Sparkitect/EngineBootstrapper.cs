using DryIoc;
using Sparkitect.Modding;
using Sparkitect.Utils;
using System;
using System.Linq;

namespace Sparkitect;

/// <summary>
/// Manages the engine initialization process, from application startup to the transition to the first game state.
/// </summary>
public class EngineBootstrapper
{
    private IContainer? _coreContainer;
    private IModManager? _modManager;
    private ICliArgumentHandler? _cliArgumentHandler;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    public static void Main(string[] args)
    {
        var bootstrapper = new EngineBootstrapper();

        try
        {
            bootstrapper.BuildCoreContainer();
            bootstrapper.InitializeCliArguments(args);
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
        _coreContainer.Register<ICliArgumentHandler, CliArgumentHandler>(Reuse.Singleton);
        _coreContainer.Register<IModManager, ModManager>(Reuse.Singleton);
        _coreContainer.Register<IIdentificationManager, IdentificationManager>(Reuse.Singleton);

        // Resolve the mod manager for later use
        _cliArgumentHandler = _coreContainer.Resolve<ICliArgumentHandler>();
        _modManager = _coreContainer.Resolve<IModManager>();
    }

    /// <summary>
    /// Initializes the CLI argument handler with command-line arguments
    /// </summary>
    public void InitializeCliArguments(string[] args)
    {
        if (_cliArgumentHandler is null)
        {
            throw new InvalidOperationException("CLI argument handler has not been initialized");
        }

        _cliArgumentHandler.Initialize(args);
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
        _modManager.LoadMods(_modManager.DiscoveredArchives.Select(a => a.Id).ToArray());
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

        var container = _modManager.CurrentCoreContainer;

        var registryManager = container.Resolve<IRegistryManager>(IfUnresolved.ReturnDefaultIfNotRegistered);

        if (registryManager is null)
        {
            throw new InvalidOperationException("Registry manager has not been initialized");
        }
        
        registryManager.ProcessRegistry();
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
        _coreContainer?.Dispose();
    }
}