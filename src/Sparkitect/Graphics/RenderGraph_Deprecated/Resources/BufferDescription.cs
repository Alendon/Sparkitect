using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Describes a device storage buffer: the per-element byte size and the initial element
/// count. The backing's byte size is <see cref="ElementStride"/> * <see cref="InitialCapacity"/>,
/// grown later by the manager's next-power-of-two policy. Carries no format or fill — buffers
/// have neither. Shared by shared-buffer registration and inline pass-local backings.
/// </summary>
/// <param name="ElementStride">Per-element byte size.</param>
/// <param name="InitialCapacity">Initial element count the backing is sized for.</param>
[PublicAPI]
public readonly record struct BufferDescription(
    ulong ElementStride,
    ulong InitialCapacity);
