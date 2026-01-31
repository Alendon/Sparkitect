using System.Numerics;
using ColorProviderMod;
using Sparkitect.Modding;

namespace BackgroundColorMod;

/// <summary>
/// Isolated integration class for ColorProviderMod.
/// All references to ColorProviderMod types are contained here.
/// </summary>
/// <remarks>
/// This class demonstrates the "drawbridge pattern" from Phase 16:
/// - Only instantiated after IsModLoaded("color_provider_mod") check passes
/// - All ColorProviderMod type references isolated to this file
/// - Prevents TypeLoadException when ColorProviderMod is not present
/// </remarks>
[OptionalModDependent("color_provider_mod")]
internal class ColorProviderIntegration
{
    /// <summary>
    /// Gets the current cycling color from ColorProviderMod.
    /// </summary>
    public Vector3 GetColor()
    {
        return ColorProvider.GetCyclingColor();
    }
}
