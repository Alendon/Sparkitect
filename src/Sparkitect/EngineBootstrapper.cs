using Sparkitect.DI.Exceptions;
using Sparkitect.Modding;
using Sparkitect.Utils;
using InterpolatedParsing;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Sparkitect.DI.Container;
using Sparkitect.GameState;

namespace Sparkitect;

/// <summary>
/// Manages the engine initialization process, from application startup to the transition to the first game state.
/// </summary>
public class EngineBootstrapper
{
    private ICoreContainer? _coreContainer;
    private IModManager? _modManager;
    private ICliArgumentHandler? _cliArgumentHandler;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    public static void Main(string[] args)
    {
        InitializeLogger(args);
        
        
        // Static reference to Log initializes the logger
        Log.Information("Sparkitect engine starting up");
        
        var bootstrapper = new EngineBootstrapper();

        try
        {
            Log.Information("Building core container");
            bootstrapper.BuildCoreContainer();
            
            Log.Information("Initializing CLI arguments");
            bootstrapper.InitializeCliArguments(args);
            
            Log.Information("Loading root mods");
            bootstrapper.LoadRootMods();
            
            Log.Information("Processing registries");
            bootstrapper.ProcessRegistries();
            
            Log.Information("Starting game loop");
            bootstrapper.RunGame();
        }
        finally
        {
            Log.Information("Cleaning up resources");
            bootstrapper.CleanUp();
        }
    }

    private const string LogDirectoryPath = "logs";
    private const string LogDirArgName = "logDir";
    
    private static void InitializeLogger(string[] args)
    {
        var logDir = LogDirectoryPath;
        
        var logDirArg = args.FirstOrDefault(x => x.StartsWith($"-{LogDirArgName}="));
        if (logDirArg is not null)
        {
            string logDirArgNameStub = String.Empty;
            InterpolatedParser.Parse(logDirArg, $"-{logDirArgNameStub}={logDir}");
        }

        logDir = Path.GetFullPath(logDir);
        
        
        // Create logs directory if it doesn't exist
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Configure and initialize Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.File(new CompactJsonFormatter(), $"{logDir}/{timestamp}.log", rollOnFileSizeLimit: true,
                flushToDiskInterval: TimeSpan.FromMinutes(1))
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}][{ModName}/{ClassName}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

    }

    /// <summary>
    /// Builds the core container with essential services needed for engine initialization
    /// </summary>
    public void BuildCoreContainer()
    {
        ICoreContainerBuilder builder = new CoreContainerBuilder(null);

        // Register service factories for base container services
        builder.Register<CliArgumentHandler_Factory>();
        builder.Register<IdentificationManager_Factory>();
        builder.Register<ModManager_Factory>();
        builder.Register<GameStateManager_Factory>();

        try
        {
            var container = builder.Build();
            _coreContainer = container; 

            // Resolve essential services
            _cliArgumentHandler = container.Resolve<ICliArgumentHandler>();
            _modManager = container.Resolve<IModManager>();
            if (_modManager is ModManager modManager)
            {
                modManager.BaseCoreContainer = container;
            }
            
            Log.Debug("Core container built successfully");
        }
        catch (CircularDependencyException ex)
        {
            Log.Fatal(ex, "Circular dependency detected in core container");
            throw;
        }
        catch (DependencyResolutionException ex)
        {
            Log.Fatal(ex, "Failed to resolve dependency in core container");
            throw;
        }
    }

    /// <summary>
    /// Initializes the CLI argument handler with command-line arguments
    /// </summary>
    public void InitializeCliArguments(string[] args)
    {
        if (_cliArgumentHandler is null)
        {
            Log.Error("CLI argument handler is null");
            throw new InvalidOperationException("CLI argument handler has not been initialized");
        }

        _cliArgumentHandler.Initialize(args);
        Log.Debug("CLI arguments initialized with {ArgCount} arguments", args.Length);
    }

    /// <summary>
    /// Discovers and loads all mods from the mods folder
    /// </summary>
    public void LoadRootMods()
    {
        if (_modManager is null)
        {
            Log.Error("Mod manager is null");
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        // Discover and load all mods
        Log.Debug("Discovering mods");
        _modManager.DiscoverMods();
        
        var modIds = _modManager.DiscoveredArchives.Select(a => a.Id).ToArray();
        Log.Information("Loading {ModCount} mods", modIds.Length);
        
        _modManager.LoadMods(modIds);
        Log.Debug("Mods loaded successfully");
    }

    /// <summary>
    /// Processes all registries from loaded mods
    /// </summary>
    public void ProcessRegistries()
    {
        if (_modManager is null)
        {
            Log.Error("Mod manager is null");
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        var container = _modManager.CurrentCoreContainer;

        if (container.TryResolve(out IRegistryManager? registryManager))
        {
            Log.Debug("Processing registries");
            registryManager.ProcessRegistry();
            Log.Debug("Registries processed successfully");
        }
        else
        {
            Log.Error("Registry manager is null");
            throw new InvalidOperationException("Registry manager has not been initialized");
        }
    }

    /// <summary>
    /// Transitions to the first game state and runs the game loop
    /// </summary>
    public void RunGame()
    {
        // Placeholder until the game state system is implemented
        Log.Information("Game is running...");

        // Try to resolve the scaffolded GameStateManager
        var container = _modManager?.CurrentCoreContainer;
        if (container is not null && container.TryResolve(out IGameStateManager? gsm))
        {
            Log.Information("GameStateManager resolved (scaffold). No loop implemented yet.");
            // gsm.SetInitial("bootstrap"); // left unset intentionally
        }
        else
        {
            Log.Debug("GameStateManager not available yet");
        }
    }

    /// <summary>
    /// Cleans up resources when the engine shuts down
    /// </summary>
    public void CleanUp()
    {
        Log.Information("Disposing core container");
        _coreContainer?.Dispose();
        
        // Flush any remaining logs
        Log.Information("Shutting down logger");
        Log.CloseAndFlush();
    }
}
