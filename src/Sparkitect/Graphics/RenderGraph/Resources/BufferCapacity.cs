using System.Numerics;
using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Pure capacity-growth math for device buffer backings.</summary>
[PublicAPI]
public static class BufferCapacity
{
    /// <summary>
    /// Returns the next power of two at least <paramref name="needed"/>, never shrinking below
    /// <paramref name="current"/>. When <paramref name="needed"/> fits the current capacity (or is
    /// zero), <paramref name="current"/> is returned unchanged.
    /// </summary>
    public static ulong NextCapacity(ulong current, ulong needed)
    {
        if (needed <= current)
            return current;

        var rounded = BitOperations.RoundUpToPowerOf2(needed);
        return rounded >= current ? rounded : current;
    }
}
