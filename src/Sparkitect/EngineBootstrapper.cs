using JetBrains.Annotations;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;

namespace Sparkitect;

/// <summary>
/// Manages the engine initialization process, from application startup to the transition to the first game state.
/// </summary>
[PublicAPI]
public class EngineBootstrapper
{
    private ICoreContainer? _coreContainer;
    private IModManager? _modManager;
    private IGameStateManager? _gameStateManager;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    public static void Main(string[] args)
    {
        // Record the entry args once so the early logger read and the CLI settings source read one
        // authoritative arg set.
        EngineEntryArguments.Set(args);

        InitializeLogger();


        // Static reference to Log initializes the logger
        Log.Information("Sparkitect engine starting up");

        var bootstrapper = new EngineBootstrapper();

        try
        {
            Log.Information("Building core container");
            bootstrapper.BuildCoreContainer();

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

    private static void InitializeLogger()
    {
        // Pre-container read of the log level and directory through the shared EarlySettings path
        // (CLI > engine-config > default) — no two-stage logger, no bootstrap reorder. Formal source
        // registration still happens later for completeness.
        var logLevel = EarlySettings.Read("log_level", EngineSettingDeclarations.LogLevel);
        var logDir = EarlySettings.Read("log_dir", EngineSettingDeclarations.LogDirectory);

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
            .MinimumLevel.Is(logLevel)
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

        // Use generated CoreModule_ServiceConfigurator for all core services
        var configurator = new Sparkitect.CompilerGenerated.GameState.CoreModule_ServiceConfigurator();
        configurator.Configure(builder, new HashSet<string>());

        try
        {
            var container = builder.Build();
            _coreContainer = container;

            // Resolve essential services
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
