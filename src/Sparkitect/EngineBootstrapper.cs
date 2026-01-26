using InterpolatedParsing;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Stateless;
using Sparkitect.Utils;

namespace Sparkitect;

/// <summary>
/// Manages the engine initialization process, from application startup to the transition to the first game state.
/// </summary>
public class EngineBootstrapper
{
    private ICoreContainer? _coreContainer;
    private IModManager? _modManager;
    private ICliArgumentHandler? _cliArgumentHandler;
    private IGameStateManager? _gameStateManager;

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

            Log.Information("Discovering mods");
            bootstrapper.DiscoverMods();
            
            Log.Information("Enter Root State");
            bootstrapper.EnterRootState();
        }
        finally
        {
            Log.Information("Cleaning up resources");
            bootstrapper.CleanUp();
        }
    }

    private void EnterRootState()
    {
        var gsm = _coreContainer!.Resolve<IGameStateManager>() as GameStateManager;
        gsm!.EnterRootState();
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

        // Wrap sinks with Identification transformation
        var wrappedConsoleSink = LoggerSinkConfiguration.Wrap(
            sink => new IdentificationInterceptSink(sink),
            cfg => cfg.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}][{ModName}/{ClassName}] {Message:lj}{NewLine}{Exception}"));

        var wrappedFileSink = LoggerSinkConfiguration.Wrap(
            sink => new IdentificationInterceptSink(sink),
            cfg => cfg.File(new CompactJsonFormatter(), $"{logDir}/{timestamp}.log",
                rollOnFileSizeLimit: true, flushToDiskInterval: TimeSpan.FromMinutes(1)));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Destructure.AsScalar<Identification>()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Sink(wrappedConsoleSink)
            .WriteTo.Sink(wrappedFileSink)
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
        builder.Register<ResourceManager_Factory>();
        builder.Register<ModManager_Factory>();
        builder.Register<RegistryManager_Factory>();
        builder.Register<GameStateManager_Factory>();
        builder.Register<ModDIService_Factory>();
        builder.Register<StatelessFunctionManager_Factory>();

        try
        {
            var container = builder.Build();
            _coreContainer = container;

            // Resolve essential services
            _cliArgumentHandler = container.Resolve<ICliArgumentHandler>();
            _modManager = container.Resolve<IModManager>();
            _gameStateManager = container.Resolve<IGameStateManager>();

            // Initialize static accessors for debugger proxy and log sink
            var identificationManager = container.Resolve<IIdentificationManager>();
            IdentificationDebuggerProxy.Instance = identificationManager;
            IdentificationInterceptSink.Instance = identificationManager;

            var internalGsm = (_gameStateManager as GameStateManager)!;
            internalGsm.RootContainer = _coreContainer;

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
    /// Discovers all available mods from the mods folder
    /// </summary>
    public void DiscoverMods()
    {
        if (_modManager is null)
        {
            Log.Error("Mod manager is null");
            throw new InvalidOperationException("Mod manager has not been initialized");
        }

        _modManager.DiscoverMods();
        Log.Debug("Discovered {ModCount} mods", _modManager.DiscoveredArchives.Count);
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
