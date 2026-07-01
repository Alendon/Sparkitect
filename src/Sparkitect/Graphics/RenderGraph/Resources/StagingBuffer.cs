using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The staging composite instance the staging pass fetches: a host-mapped <see cref="BufferResource"/> whose
/// <see cref="BufferResource.MappedData"/> is the CPU memcpy target, and the device-local
/// <see cref="BufferResource"/> that receives the host->device copy and is the same physical backing the
/// compute pass later reads. Carries no size — the byte count is data-driven at the pass.
/// </summary>
[PublicAPI]
public sealed class StagingBuffer
{
    /// <summary>The host-mapped leaf; write CPU data to its <see cref="BufferResource.MappedData"/>.</summary>
    public BufferResource Host { get; }

    /// <summary>The device-local leaf: the host->device copy destination and the shader-read backing.</summary>
    public BufferResource Device { get; }

    /// <summary>Composes the staging pair from the resolved host and device leaves.</summary>
    public StagingBuffer(BufferResource host, BufferResource device)
    {
        Host = host;
        Device = device;
    }
}
