using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The staging composite instance the staging pass fetches: a host-mapped <see cref="BufferResource"/> whose
/// <see cref="BufferResource.MappedData"/> is the CPU memcpy target, and the device-local
/// <see cref="BufferResource"/> that receives the host->device copy and is the same physical backing the
/// compute pass later reads. Size is data-driven: <see cref="Write"/> grows both leaves to the written byte
/// count (floored to a nonzero minimum) at write time through the buffer manager.
/// </summary>
[PublicAPI]
public sealed class StagingBuffer
{
    private readonly IBufferManager _manager;

    /// <summary>The host-mapped leaf; <see cref="Write"/> memcpys CPU data into its <see cref="BufferResource.MappedData"/>.</summary>
    public BufferResource Host { get; }

    /// <summary>The device-local leaf: the host->device copy destination and the shader-read backing.</summary>
    public BufferResource Device { get; }

    /// <summary>Composes the staging pair from the resolved host and device leaves plus the manager that grows them.</summary>
    public StagingBuffer(BufferResource host, BufferResource device, IBufferManager manager)
    {
        Host = host;
        Device = device;
        _manager = manager;
    }

    /// <summary>
    /// Grows both leaves to <paramref name="data"/>'s byte count (floored to the manager's nonzero minimum) and
    /// memcpys the data into the host-mapped backing. The grow is identity-preserving, so the device leaf the
    /// compute pass resolves later in the frame is the grown backing. An empty span still grows to the floor
    /// (a valid minimal buffer) and skips the memcpy.
    /// </summary>
    public unsafe void Write(ReadOnlySpan<byte> data)
    {
        _manager.GrowHostLeaf((ulong)data.Length);
        _manager.GrowDeviceLeaf((ulong)data.Length);

        if (data.IsEmpty) return;

        var destination = new Span<byte>((void*)Host.MappedData, data.Length);
        data.CopyTo(destination);
    }
}
