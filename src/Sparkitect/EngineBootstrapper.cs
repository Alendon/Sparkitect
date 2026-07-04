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
using Sparkitect.Settings.Sources;
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

        InitializeLogger(args);


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

    private static void InitializeLogger(string[] args)
    {
        // Read the log level and directory directly from CLI args + Sparkitect.yaml, reusing the same
        // parsers the CLI/engine-config sources use and the engine's own setting declarations (their CLI
        // option, default, and scalar parser) — no two-stage logger, no bootstrap reorder. Formal source
        // registration still happens later for completeness. Resolution order: CLI > engine-config > default.
        var cliArgs = CliSettingsSource.ParseArguments(args);
        var engineConfig = EngineSettingsSource.ReadWorkingDirectoryScalars();

        var levelDeclaration = EngineSettingDeclarations.LogLevel;
        var dirDeclaration = EngineSettingDeclarations.LogDirectory;

        var logLevel = ReadEarlySetting(cliArgs, engineConfig, levelDeclaration.CliOption, "log_level", levelDeclaration)
            is LogEventLevel level ? level : levelDeclaration.Default;
        var logDir = ReadEarlySetting(cliArgs, engineConfig, dirDeclaration.CliOption, "log_dir", dirDeclaration)
            is string dir ? dir : dirDeclaration.Default;

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

    // Resolves a setting's raw value early (pre-container) from CLI then engine-config, parsed against the
    // declaration's own scalar parser; returns null when neither source supplies a parseable value.
    private static object? ReadEarlySetting(
        IReadOnlyDictionary<string, CliArgValue> cliArgs,
        IReadOnlyDictionary<string, string> engineConfig,
        string? cliOption,
        string engineConfigKey,
        ISettingDeclaration declaration)
    {
        if (cliOption is not null && cliArgs.TryGetValue(cliOption, out var argument))
        {
            var raw = argument switch
            {
                CliArgValue.Flag => "true",
                CliArgValue.Single single => single.Value,
                CliArgValue.Multi multi => multi.Values.Count > 0 ? multi.Values[0] : null,
            };
            if (raw is not null && declaration.TryParseScalar(raw, out var cliValue))
            {
                return cliValue;
            }
        }

        if (engineConfig.TryGetValue(engineConfigKey, out var configRaw) &&
            declaration.TryParseScalar(configRaw, out var configValue))
        {
            return configValue;
        }

        return null;
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
