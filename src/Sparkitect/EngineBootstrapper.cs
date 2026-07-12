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
        Exception? primaryFailure = null;

        try
        {
            Log.Information("Building core container");
            bootstrapper.BuildCoreContainer();

            Log.Information("Discovering mods");
            bootstrapper.DiscoverMods();

            Log.Information("Enter Root State");
            bootstrapper.EnterRootState();
        }
        catch (Exception ex)
        {
            // Do not resume: the process boundary is terminal from here. The primary runtime cause
            // is captured and preserved through shutdown rather than being replaced by a later
            // cleanup-phase exception (the classic try/finally exception-shadowing gotcha).
            primaryFailure = ex;
            Log.Fatal(ex, "Unhandled runtime failure; entering process-boundary shutdown");
        }

        Log.Information("Cleaning up resources");
        var shutdownFailures = bootstrapper.CleanUp();

        var boundaryException = BuildBoundaryException(primaryFailure, shutdownFailures);
        if (boundaryException is not null)
            throw boundaryException;
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
    /// Runs the process-boundary shutdown sequence: attempts terminal state unwind (if a game state
    /// manager was resolved), root container disposal (if the container was built), and logger flush,
    /// in that order. Every step is attempted regardless of an earlier step's failure; a step whose
    /// prerequisite object was never created is skipped as unsafe rather than attempted. Returns every
    /// failure observed, in attempt order, for the caller to preserve alongside the primary cause.
    /// </summary>
    public IReadOnlyList<Exception> CleanUp()
    {
        return RunBoundarySequence(
            hasGameStateManager: _gameStateManager is not null,
            attemptTerminalUnwind: () => _gameStateManager!.Shutdown(),
            hasCoreContainer: _coreContainer is not null,
            attemptRootCleanup: () => _coreContainer!.Dispose(),
            attemptLoggerFlush: () => Log.CloseAndFlush());
    }

    /// <summary>
    /// Pure boundary-collector algorithm: attempts each step exactly once in order, skipping a step
    /// whose prerequisite is absent (unsafe to attempt) while still continuing the remaining,
    /// unrelated steps. Never rethrows - failures are captured and returned instead of interrupting
    /// the sequence, so a later step's exception can never shadow an earlier one. The logger cannot
    /// be trusted to report its own flush failure, so that one alone is also written to
    /// <paramref name="stderr"/> (defaults to <see cref="Console.Error"/>; injectable for tests).
    /// </summary>
    internal static IReadOnlyList<Exception> RunBoundarySequence(
        bool hasGameStateManager,
        Action attemptTerminalUnwind,
        bool hasCoreContainer,
        Action attemptRootCleanup,
        Action attemptLoggerFlush,
        TextWriter? stderr = null)
    {
        var failures = new List<Exception>();

        if (hasGameStateManager)
            Attempt(attemptTerminalUnwind, "Terminal state unwind failed.", failures);

        if (hasCoreContainer)
            Attempt(attemptRootCleanup, "Root container cleanup failed.", failures);

        // Always attempted last and unconditionally: diagnostics are worthless if nothing got flushed.
        Attempt(attemptLoggerFlush, "Logger flush failed.", failures, stderr ?? Console.Error);

        return failures;
    }

    private static void Attempt(Action action, string failureMessage, List<Exception> failures, TextWriter? stderr = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            failures.Add(new InvalidOperationException(failureMessage, ex));
            stderr?.WriteLine($"FATAL: {failureMessage} {ex}");
        }
    }

    /// <summary>
    /// Composes the final process-boundary outcome: the primary runtime failure (if any) stays first
    /// and is never replaced by a shutdown-phase failure; shutdown failures are appended, and the
    /// whole set is flattened into one aggregate. Returns null only when nothing failed.
    /// </summary>
    internal static Exception? BuildBoundaryException(Exception? primaryFailure, IReadOnlyList<Exception> shutdownFailures)
    {
        if (primaryFailure is null && shutdownFailures.Count == 0)
            return null;

        var causes = primaryFailure is null
            ? shutdownFailures
            : new[] { primaryFailure }.Concat(shutdownFailures).ToArray();

        return new AggregateException("Sparkitect engine terminated unsuccessfully.", causes).Flatten();
    }
}
