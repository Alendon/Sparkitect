using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Per-graph push target for the <see cref="EntityListResource"/>. Owns the host-visible/mapped
/// <see cref="VkBuffer"/> that the staging pass copies from, and stores the current published instance.
/// Per the staging-owns-the-copy model, <see cref="Publish"/> only records the current instance and grows
/// the host buffer if needed — it never fills the buffer or records a GPU copy (that is the staging pass's
/// job). Its <see cref="Current"/> and <see cref="HostBuffer"/> accessors are public so a mod-assembly
/// staging pass (a separate assembly with no InternalsVisibleTo grant) can read them for the per-frame fill.
/// </summary>
[GraphLocal<IEntityListResourceManager>]
public sealed class EntityListResourceManager
    : IEntityListResourceManager, IGraphPushTargetFor<EntityListResource>, IDisposable
{
    private static readonly ulong ElementStride = (ulong)Marshal.SizeOf<GpuRenderEntity>();

    private readonly IVulkanContext _vulkanContext;
    private EntityListResource? _current;
    private VkBuffer? _hostBuffer;
    private ulong _capacityElements;

    public EntityListResourceManager(IVulkanContext vulkanContext)
    {
        _vulkanContext = vulkanContext;
    }

    /// <summary>The current published entity list, or null before the first publish.</summary>
    public EntityListResource? Current => _current;

    /// <summary>
    /// The host-visible/mapped staging buffer the staging pass writes into and copies from; allocated lazily
    /// on first publish and grown as needed.
    /// </summary>
    public VkBuffer HostBuffer =>
        _hostBuffer ?? throw new InvalidOperationException(
            "EntityListResourceManager.HostBuffer: no list has been published yet — the host buffer is " +
            "allocated on first Publish.");

    public void Publish(EntityListResource value)
    {
        EnsureCapacity((ulong)value.Count);
        _current = value;
    }

    private void EnsureCapacity(ulong neededElements)
    {
        var newCapacity = BufferCapacity.NextCapacity(_capacityElements, neededElements);
        if (_hostBuffer is not null && newCapacity == _capacityElements)
            return;

        _hostBuffer?.Dispose();
        _capacityElements = newCapacity == 0 ? BufferCapacity.NextCapacity(0, 1) : newCapacity;

        var result = _vulkanContext.CreateMappedStorageBuffer(_capacityElements * ElementStride);
        if (result is not Result<VkBuffer, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                "EntityListResourceManager: CreateMappedStorageBuffer failed " +
                $"({((Result<VkBuffer, VkApiResult>.Error)result).Value}).");

        _hostBuffer = ok.Value;
    }

    public void Dispose()
    {
        _hostBuffer?.Dispose();
        _hostBuffer = null;
        _current = null;
    }
}
