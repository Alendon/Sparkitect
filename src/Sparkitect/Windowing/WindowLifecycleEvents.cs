using JetBrains.Annotations;
using Sparkitect.Events;

namespace Sparkitect.Windowing;

/// <summary>
/// The engine's own managed-window lifecycle event declarations, registered through the standard
/// <see cref="EventRegistry"/> path. <see cref="WindowCreated"/> and <see cref="WindowClosing"/> carry the
/// live window so subscribers can act against it; <see cref="WindowDestroyed"/> is identity-only — the
/// window is already gone, subscribers must drop their references, not read from it.
/// </summary>
[PublicAPI]
public static class WindowLifecycleEvents
{
    /// <summary>Published when a managed window is created and tracked.</summary>
    [EventRegistry.RegisterEvent("window_created")]
    public static IEventDefinition<ISparkitWindow> WindowCreated => new EventDefinition<ISparkitWindow>();

    /// <summary>Published while a managed window is still fully alive, immediately before teardown begins. Subscribers do real cleanup here (e.g. unregister sources).</summary>
    [EventRegistry.RegisterEvent("window_closing")]
    public static IEventDefinition<ISparkitWindow> WindowClosing => new EventDefinition<ISparkitWindow>();

    /// <summary>Published after a managed window has been disposed and untracked. Identity-only — drop references now, no live-window access is possible.</summary>
    [EventRegistry.RegisterEvent("window_destroyed")]
    public static IEventDefinition<WindowIdentity> WindowDestroyed => new EventDefinition<WindowIdentity>();
}

/// <summary>
/// Opaque identity for a window that has already been disposed. Carries no live window access — value
/// only, for subscribers that need to recognize which of their own tracked references to drop.
/// </summary>
/// <param name="Id">Stable identifier assigned to the window at creation.</param>
[PublicAPI]
public readonly record struct WindowIdentity(int Id);
