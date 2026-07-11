using JetBrains.Annotations;
using Serilog;
using Silk.NET.Input;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Events;
using Sparkitect.GameState;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using Sparkitect.Windowing;
using Sparkitect.Windowing.Input;

namespace Sparkitect.WindowInput;

/// <summary>
/// The auto-composed bridge between Windowing and Input (D-01/D-02). It declares no direct
/// <see cref="Requires"/> edge to either module -- it activates only once BOTH are present in a
/// composed state's set (<see cref="ActivatesWith"/>, the AND-fixpoint) -- and wires each managed
/// window's keyboard into Input's device-agnostic source seam through the exact same public
/// control-plane API (<see cref="IInputManager.RegisterSource{TValue,TRaw}"/>) a gamepad mod would use
/// later to add its own source: no privileged engine path. Being the only party that knows both
/// modules, it is also the only place their per-frame execution order can be expressed
/// (<see cref="MaintainWindowInputFrameOrder"/>).
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("window_input")]
public sealed partial class WindowInputModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires => [];

    /// <inheritdoc/>
    public override IReadOnlyList<Identification> ActivatesWith =>
        [StateModuleID.Sparkitect.Windowing, StateModuleID.Sparkitect.Input];

    /// <summary>
    /// Wires managed-window keyboards into Input's source seam, race-free by construction (D-15): it
    /// BOTH subscribes to <see cref="WindowLifecycleEvents.WindowCreated"/> for windows created from now
    /// on AND catch-up-queries <see cref="IWindowManagerStateFacade.TrackedWindows"/> for windows already
    /// tracked before this subscription existed -- subscribe-vs-create ordering stops mattering. Also
    /// subscribes to <see cref="WindowLifecycleEvents.WindowClosing"/> to unregister a window's source
    /// while it is still fully alive (real cleanup, D-11). Re-running this on oscillation replay
    /// (state re-enter without recreation) is safe: subscriptions are reset before being re-established,
    /// and the catch-up query skips windows already tracked (D-26).
    /// </summary>
    [OnFrameEnterScheduling]
    [TransitionFunction("wire_window_keyboards")]
    private static void WireWindowKeyboards(
        IEventManager eventManager,
        IInputManager inputManager,
        IWindowManagerStateFacade windowManager,
        IWindowInputSourceRegistry sourceRegistry)
    {
        var createdSubscription = eventManager.Subscribe(
            EventID.Sparkitect.WindowCreated,
            window => RegisterWindowKeyboard(window, inputManager, sourceRegistry));

        var closingSubscription = eventManager.Subscribe(
            EventID.Sparkitect.WindowClosing,
            window => sourceRegistry.TryRelease(window));

        sourceRegistry.ResetSubscriptions(createdSubscription, closingSubscription);

        foreach (var window in windowManager.TrackedWindows)
        {
            RegisterWindowKeyboard(window, inputManager, sourceRegistry);
        }
    }

    private static void RegisterWindowKeyboard(
        ISparkitWindow window, IInputManager inputManager, IWindowInputSourceRegistry sourceRegistry)
    {
        // Idempotent under oscillation replay (D-26): the live subscription and the catch-up query can
        // both reach the same window (e.g. a window created just before this enter re-runs).
        if (sourceRegistry.IsTracked(window)) return;

        var provider = new KeyboardSourceProvider((SparkitKeyboard)window.Keyboard);
        var source = inputManager.RegisterSource<Key, bool>(provider);
        sourceRegistry.Track(window, source);
    }

    /// <summary>
    /// Tears down the bridge's wiring on state exit: unsubscribes from the window lifecycle events and
    /// disposes any still-registered sources. Live handles at this point are residuals -- windows that
    /// closed via a path other than <see cref="IWindowManager.DestroyWindow"/>, or a teardown ordering
    /// gap -- so they are auto-cleaned with a provenance warning rather than throwing (D-27): fail-loud
    /// is for precise, targeted issues at the offending call, not non-corrupting teardown residuals.
    /// </summary>
    [OnFrameExitScheduling]
    [TransitionFunction("unwire_window_keyboards")]
    private static void UnwireWindowKeyboards(IWindowInputSourceRegistry sourceRegistry) =>
        sourceRegistry.ReleaseAll();

    /// <summary>
    /// Pure cross-module ordering anchor (D-15). Input and Windowing may never declare a
    /// <see cref="Requires"/> edge against each other (D-01), so the bridge -- the only party knowing
    /// both -- is the sole place their transitive per-frame order can be expressed: this function carries
    /// no data-plane work of its own, only the ordering constraints, placing
    /// <c>pump_windows -&gt; (this) -&gt; build_input_snapshot</c> in the per-frame execution graph.
    /// </summary>
    [PerFrameFunction("maintain_window_input_frame_order")]
    [PerFrameScheduling]
    [OrderAfter<WindowingModule.PumpWindowsFunc>]
    [OrderBefore<InputModule.BuildInputSnapshotFunc>]
    public static void MaintainWindowInputFrameOrder()
    {
    }
}

/// <summary>
/// Bridge-internal bookkeeping (never mod-facing): the window lifecycle event subscriptions plus the
/// per-window registered keyboard sources, so <see cref="WindowInputModule"/>'s static, DI-threaded
/// transition functions (SPARK0401/0406 -- no static/instance state on the module itself) have somewhere
/// to keep them between the enter and exit transitions. A fresh instance is not guaranteed across
/// oscillation (state re-enter without recreation reuses this same instance), so every mutation here is
/// written to be safe under repeat calls.
/// </summary>
internal interface IWindowInputSourceRegistry
{
    /// <summary>Whether <paramref name="window"/> already has a tracked, registered source.</summary>
    bool IsTracked(ISparkitWindow window);

    /// <summary>Tracks <paramref name="source"/> as the currently registered source for <paramref name="window"/>.</summary>
    void Track(ISparkitWindow window, SourceBinding source);

    /// <summary>Disposes and untracks the source for <paramref name="window"/>, if any is tracked.</summary>
    bool TryRelease(ISparkitWindow window);

    /// <summary>
    /// Disposes the previously-tracked <c>WindowCreated</c>/<c>WindowClosing</c> subscriptions (a no-op the
    /// first time, since a never-set <see cref="EventBinding"/> disposes harmlessly) before storing the new
    /// ones -- keeps re-subscription idempotent across oscillation replay.
    /// </summary>
    void ResetSubscriptions(EventBinding windowCreated, EventBinding windowClosing);

    /// <summary>
    /// Disposes the tracked subscriptions and every still-registered source (warning on each residual, D-27),
    /// then clears all bookkeeping. Called from the exit transition.
    /// </summary>
    void ReleaseAll();
}

[StateService<IWindowInputSourceRegistry, WindowInputModule>]
internal sealed class WindowInputSourceRegistry : IWindowInputSourceRegistry
{
    private readonly Dictionary<ISparkitWindow, SourceBinding> _sourcesByWindow = new();
    private EventBinding _windowCreatedSubscription;
    private EventBinding _windowClosingSubscription;

    /// <inheritdoc/>
    public bool IsTracked(ISparkitWindow window) => _sourcesByWindow.ContainsKey(window);

    /// <inheritdoc/>
    public void Track(ISparkitWindow window, SourceBinding source) => _sourcesByWindow[window] = source;

    /// <inheritdoc/>
    public bool TryRelease(ISparkitWindow window)
    {
        if (!_sourcesByWindow.Remove(window, out var source)) return false;
        source.Dispose();
        return true;
    }

    /// <inheritdoc/>
    public void ResetSubscriptions(EventBinding windowCreated, EventBinding windowClosing)
    {
        _windowCreatedSubscription.Dispose();
        _windowClosingSubscription.Dispose();
        _windowCreatedSubscription = windowCreated;
        _windowClosingSubscription = windowClosing;
    }

    /// <inheritdoc/>
    public void ReleaseAll()
    {
        _windowCreatedSubscription.Dispose();
        _windowClosingSubscription.Dispose();
        _windowCreatedSubscription = default;
        _windowClosingSubscription = default;

        foreach (var (window, source) in _sourcesByWindow)
        {
            Log.Warning(
                "WindowInputModule: auto-cleaning a residual keyboard source for window '{Title}' at state " +
                "teardown -- it should have been released via WindowClosing before this point.",
                window.Title);
            source.Dispose();
        }

        _sourcesByWindow.Clear();
    }
}
