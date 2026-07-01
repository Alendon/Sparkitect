using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The concrete graph-local backing provider for storage-buffer leaves. Owns one host-mapped and one
/// device-local backing, each grown-to-fit on resolve and reused for the graph's lifetime. Growth disposes
/// the outgrown backing; a resolve within capacity reuses the leaf so its barrier state spans uses.
/// </summary>
[PublicAPI]
[GraphLocal<IBufferManager, IRenderGraph>]
public sealed class BufferManager : IBufferManager
{
    private readonly IVulkanContext _vulkanContext;

    // Allocated grow-to-fit and reused for the graph's lifetime; not the cross-pass identity source.
    private BufferResource? _hostLeaf;
    private BufferResource? _deviceLeaf;

    public BufferManager(IVulkanContext vulkanContext) => _vulkanContext = vulkanContext;

    /// <inheritdoc/>
    public ulong CurrentByteSize { get; set; }

    /// <inheritdoc/>
    public BufferResource ResolveHostLeaf(ulong byteSize)
        => Resolve(ref _hostLeaf, byteSize, host: true);

    /// <inheritdoc/>
    public BufferResource ResolveDeviceLeaf(ulong byteSize)
        => Resolve(ref _deviceLeaf, byteSize, host: false);

    // Grows the backing when the request exceeds current capacity (disposing the old backing), else reuses the
    // cached leaf and refreshes its logical size — reuse preserves the carried barrier state across uses.
    private BufferResource Resolve(ref BufferResource? leaf, ulong byteSize, bool host)
    {
        if (leaf is not null && leaf.Backing.Size >= byteSize)
        {
            leaf.ByteSize = byteSize;
            return leaf;
        }

        leaf?.Backing.Dispose();

        var result = host
            ? _vulkanContext.CreateMappedStorageBuffer(byteSize)
            : _vulkanContext.CreateDeviceStorageBuffer(byteSize);
        if (result is not Result<VkBuffer, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"BufferManager.Resolve: {(host ? "CreateMappedStorageBuffer" : "CreateDeviceStorageBuffer")} " +
                $"failed ({((Result<VkBuffer, VkApiResult>.Error)result).Value}).");

        leaf = new BufferResource(ok.Value, byteSize);
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
