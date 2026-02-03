using System.Numerics;
using PongMod;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace BackgroundColorMod;

/// <summary>
/// Adds background color modification to PongState.
/// Demonstrates cross-compilation state method registration.
/// </summary>
public partial class BackgroundColorState
{
    private static readonly Vector3 FallbackColor = new(0.15f, 0.1f, 0.2f);  // Dark purple

    /// <summary>
    /// Updates Pong background color each frame.
    /// Uses cycling color if ColorProviderMod is loaded, otherwise fixed color.
    /// </summary>
    [PerFrameFunction("background_color_update")]
    [PerFrameScheduling]
    [ParentId<PongModule>]
    [OrderBefore<PongState.PongFrameFunc>]
    public static void UpdateBackgroundColor(
        IPongRuntimeService pongRuntime,
        IGameStateManager gsm)
    {
        ApplyCyclingColor(pongRuntime, gsm);
    }

    /// <summary>
    /// Guarded entry point for ColorProviderMod integration.
    /// Only called after IsModLoaded check passes.
    /// </summary>
    [ModLoadedGuard("color_provider_mod")]
    private static void ApplyCyclingColor(IPongRuntimeService pongRuntime, IGameStateManager gsm)
    {
        if (gsm.IsModLoaded("color_provider_mod"))
        {
            var integration = new ColorProviderIntegration();
            pongRuntime.BackgroundColor = integration.GetColor();
        }
        else
        {
            pongRuntime.BackgroundColor = FallbackColor;
        }
    }
}
