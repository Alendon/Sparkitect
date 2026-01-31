using System.Numerics;

namespace ColorProviderMod;

/// <summary>
/// Provides time-based color cycling for visual effects.
/// </summary>
public static class ColorProvider
{
    private const float DefaultCycleDuration = 5f;

    /// <summary>
    /// Gets a color that cycles through the hue spectrum over time.
    /// </summary>
    /// <param name="cycleDurationSeconds">Seconds for one complete hue cycle. Default: 5 seconds.</param>
    /// <returns>RGB color as Vector3, values in [0,1] range.</returns>
    public static Vector3 GetCyclingColor(float cycleDurationSeconds = DefaultCycleDuration)
    {
        var totalSeconds = DateTime.Now.TimeOfDay.TotalSeconds;
        var hue = (float)(totalSeconds % cycleDurationSeconds) / cycleDurationSeconds * 360f;
        return HsvToRgb(hue, 0.6f, 0.25f);  // Saturation 60%, Value 25% for dark background
    }

    /// <summary>
    /// Converts HSV color to RGB.
    /// </summary>
    /// <param name="h">Hue in degrees [0, 360]</param>
    /// <param name="s">Saturation [0, 1]</param>
    /// <param name="v">Value [0, 1]</param>
    /// <returns>RGB color as Vector3, values in [0,1] range.</returns>
    public static Vector3 HsvToRgb(float h, float s, float v)
    {
        var c = v * s;
        var x = c * (1 - MathF.Abs((h / 60f) % 2 - 1));
        var m = v - c;

        Vector3 rgb = (h % 360) switch
        {
            < 60 => new Vector3(c, x, 0),
            < 120 => new Vector3(x, c, 0),
            < 180 => new Vector3(0, c, x),
            < 240 => new Vector3(0, x, c),
            < 300 => new Vector3(x, 0, c),
            _ => new Vector3(c, 0, x)
        };

        return new Vector3(rgb.X + m, rgb.Y + m, rgb.Z + m);
    }
}
