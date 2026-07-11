using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Windowing;

/// <summary>State module that provides windowing and input. Depends on the core module.</summary>
[ModuleRegistry.RegisterModule("windowing")]
[PublicAPI]
public partial class WindowingModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires => [StateModuleID.Sparkitect.Event];

    /// <summary>
    /// The engine's first per-frame function: pumps every currently tracked managed window once per frame
    /// (D-09). Reaches tracked windows only through the DI-resolved state facade — the tracking state itself
    /// lives in <c>WindowManager</c>, never a static field.
    /// </summary>
    [PerFrameFunction("pump_windows")]
    [PerFrameScheduling]
    public static void PumpWindows(IWindowManagerStateFacade windowManager) => windowManager.PumpTrackedWindows();
}
