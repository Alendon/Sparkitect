using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;

namespace Sparkitect.Windowing;

/// <summary>
/// Whether a window is tracked and pumped by Windowing. Tracked windows are never OWNED by Windowing —
/// lifetime authority always stays with the creator; managed mode only adds per-frame event pumping and
/// lifecycle event publication (<see cref="IWindowManager.DestroyWindow"/> is the only valid teardown path
/// for a managed window).
/// </summary>
[PublicAPI]
public enum WindowManagementMode
{
    /// <summary>Fully manual: no tracking, no per-frame pump, no lifecycle events. Caller polls and disposes
    /// the window directly — today's contract, unchanged.</summary>
    Unmanaged,

    /// <summary>Tracked and pumped once per frame by the engine's <c>pump_windows</c> function; must be torn
    /// down through <see cref="IWindowManager.DestroyWindow"/>, never a direct <see cref="IDisposable.Dispose"/>.</summary>
    Managed,
}

/// <summary>
/// Creates and tracks application windows and reports the Vulkan instance extensions the windowing
/// backend requires.
/// </summary>
[StateFacade<IWindowManagerStateFacade>]
[PublicAPI]
public interface IWindowManager
{
    /// <summary>Creates a window with the given title, size, and optional swapchain configuration.</summary>
    /// <param name="title">Initial window title.</param>
    /// <param name="width">Initial client-area width in pixels.</param>
    /// <param name="height">Initial client-area height in pixels.</param>
    /// <param name="config">Optional swapchain configuration; defaults are used when null.</param>
    /// <param name="managementMode">
    /// <see cref="WindowManagementMode.Unmanaged"/> (default) keeps today's fully manual contract: no
    /// tracking, no pump, no lifecycle events, caller disposes directly. <see cref="WindowManagementMode.Managed"/>
    /// tracks the window (never owns it), pumps it once per frame, and requires <see cref="DestroyWindow"/> for teardown.
    /// </param>
    ISparkitWindow CreateWindow(
        string title,
        int width,
        int height,
        SwapchainConfig? config = null,
        WindowManagementMode managementMode = WindowManagementMode.Unmanaged);

    /// <summary>
    /// Creates a game window sized from the window-size settings and configured with the vsync setting's
    /// present mode. The engine reads the size/vsync values inline, so callers pass only a title. Always
    /// created in <see cref="WindowManagementMode.Managed"/> mode.
    /// </summary>
    /// <param name="title">Initial window title.</param>
    ISparkitWindow CreateGameWindow(string title);

    /// <summary>
    /// Tears down a managed window: publishes <c>WindowClosing</c> while it is still fully alive (subscribers
    /// do real cleanup here), disposes it, untracks it, then publishes <c>WindowDestroyed</c> (identity-only —
    /// no live-window access). This is the only valid teardown path for a window created with
    /// <see cref="WindowManagementMode.Managed"/>; calling <see cref="IDisposable.Dispose"/> on it directly
    /// throws.
    /// </summary>
    /// <param name="window">A currently-tracked managed window.</param>
    void DestroyWindow(ISparkitWindow window);

    /// <summary>The Vulkan instance extensions the windowing backend requires.</summary>
    IReadOnlyList<string> GetRequiredVulkanExtensions();
}

/// <summary>
/// State-facade marker for <see cref="IWindowManager"/>. Exposes the engine-internal per-frame pump surface —
/// not part of the general mod-facing API, resolved only by the <c>pump_windows</c> stateless function.
/// </summary>
[FacadeFor<IWindowManager>]
[PublicAPI]
public interface IWindowManagerStateFacade
{
    /// <summary>Pumps events (<see cref="ISparkitWindow.PollEvents"/>) for every currently tracked managed
    /// window. Called once per frame by the engine's <c>pump_windows</c> function.</summary>
    void PumpTrackedWindows();

    /// <summary>
    /// Every currently tracked managed window. Exists so a race-free catch-up query (D-15) can enumerate
    /// windows that were already tracked before a subscriber's <c>WindowCreated</c> subscription existed —
    /// the Windowing/Input bridge is the intended consumer, resolved via this state-facade-exclusive surface,
    /// never the general mod-facing <see cref="IWindowManager"/>.
    /// </summary>
    IReadOnlyList<ISparkitWindow> TrackedWindows { get; }
}
