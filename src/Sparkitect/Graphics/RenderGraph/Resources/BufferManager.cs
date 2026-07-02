using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The concrete graph-local backing provider for storage-buffer leaves. Owns one host-mapped and one
/// device-local backing, grown-to-fit at write time and reused for the graph's lifetime. Growth swaps the
/// new backing into the same leaf object (identity-preserving) and disposes the outgrown backing; a request
/// within capacity reuses the leaf so its barrier state spans uses. A zero-byte request is a wiring defect
/// and throws; a nonzero floor keeps an empty-push frame's leaf bindable.
/// </summary>
[PublicAPI]
[GraphLocal<IBufferManager, IRenderGraph>]
public sealed class BufferManager : IBufferManager
{
    // The minimal backing size so an empty-push frame still yields a bindable leaf for the descriptor/compute-read.
    private const ulong MinBackingBytes = 256;

    private readonly IVulkanContext _vulkanContext;

    private BufferResource? _hostLeaf;
    private BufferResource? _deviceLeaf;

    public BufferManager(IVulkanContext vulkanContext) => _vulkanContext = vulkanContext;

    /// <inheritdoc/>
    public BufferResource ResolveHostLeaf() => GrowTo(ref _hostLeaf, MinBackingBytes, host: true);

    /// <inheritdoc/>
    public BufferResource ResolveDeviceLeaf() => GrowTo(ref _deviceLeaf, MinBackingBytes, host: false);

    /// <inheritdoc/>
    public BufferResource GrowHostLeaf(ulong byteSize)
        => GrowTo(ref _hostLeaf, Math.Max(byteSize, MinBackingBytes), host: true);

    /// <inheritdoc/>
    public BufferResource GrowDeviceLeaf(ulong byteSize)
        => GrowTo(ref _deviceLeaf, Math.Max(byteSize, MinBackingBytes), host: false);

    // Grows the backing when the request exceeds current capacity, swapping the new backing into the SAME leaf
    // object and disposing the old one; within capacity it reuses the leaf and refreshes its logical size.
    private BufferResource GrowTo(ref BufferResource? leaf, ulong byteSize, bool host)
    {
        if (byteSize == 0)
            throw new InvalidOperationException(
                "BufferManager.GrowTo: a zero-byte storage buffer request is always a wiring defect. " +
                "The write path must floor the request to a nonzero minimum before resolving.");

        if (leaf is not null && leaf.Backing.Size >= byteSize)
        {
            leaf.ByteSize = byteSize;
            return leaf;
        }

        var result = host
            ? _vulkanContext.CreateMappedStorageBuffer(byteSize)
            : _vulkanContext.CreateDeviceStorageBuffer(byteSize);
        if (result is not Result<VkBuffer, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"BufferManager.GrowTo: {(host ? "CreateMappedStorageBuffer" : "CreateDeviceStorageBuffer")} " +
                $"failed ({((Result<VkBuffer, VkApiResult>.Error)result).Value}).");

        if (leaf is null)
        {
            leaf = new BufferResource(ok.Value, byteSize);
        }
        else
        {
            leaf.Backing.Dispose();
            leaf.SwapBacking(ok.Value, byteSize);
        }

        return leaf;
    }

    /// <inheritdoc/>
    public void DisposeBuffers()
    {
        _hostLeaf?.Backing.Dispose();
        _deviceLeaf?.Backing.Dispose();
        _hostLeaf = null;
        _deviceLeaf = null;
    }
}
