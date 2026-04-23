using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// <see cref="IVmaService"/> implementation registered via
/// <c>[StateService&lt;IVmaService, VulkanModule&gt;]</c>. The <see cref="DefaultAllocator"/>
/// is eagerly constructed by <see cref="Initialize"/> (invoked by the <c>create_vma</c>
/// transition after <c>create_device</c>) and disposed by <see cref="Dispose"/> (invoked
/// by the <c>destroy_vma</c> transition before <c>destroy_device</c>).
/// </summary>
[StateService<IVmaService, VulkanModule>]
public sealed class VmaService : IVmaService
{
    private ManagedVmaAllocator? _defaultAllocator;
    private readonly IObjectTracker<ManagedVmaAllocator> _allocatorTracker = new ObjectTracker<ManagedVmaAllocator>();

    /// <summary>DI-injected Vulkan context; supplies the instance/physicalDevice/device handles.</summary>
    public required IVulkanContext VulkanContext { private get; init; }

    /// <inheritdoc />
    public ManagedVmaAllocator DefaultAllocator
        => _defaultAllocator ?? throw new InvalidOperationException(
               "VmaService.DefaultAllocator accessed before the create_vma transition has run. " +
               "Ensure the owning state's Vulkan module has completed bring-up before resolving VMA resources.");

    /// <inheritdoc />
    public void Initialize()
    {
        _defaultAllocator = new ManagedVmaAllocator(
            VulkanContext.VkInstance.Handle,
            VulkanContext.VkPhysicalDevice.PhysicalDevice,
            VulkanContext.VkDevice.Handle,
            Vk.Version13);
    }

    /// <inheritdoc />
    public ManagedVmaAllocator CreateAllocator([InjectCallerContext] CallerContext callerContext = default)
    {
        return new ManagedVmaAllocator(
            VulkanContext.VkInstance.Handle,
            VulkanContext.VkPhysicalDevice.PhysicalDevice,
            VulkanContext.VkDevice.Handle,
            Vk.Version13,
            serviceTracker: _allocatorTracker,
            trackingCallsite: callerContext);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        var leakedCount = _allocatorTracker.Count;
        if (leakedCount > 0)
        {
            Log.Warning("VMA on-demand allocator leaks detected: {Count} allocator(s) not disposed", leakedCount);
            foreach (var (allocator, callsite) in _allocatorTracker.GetTrackingEntries())
            {
                Log.Warning("  Leaked {Type} created at {Callsite}",
                    allocator.GetType().Name, callsite);
            }
        }

        _defaultAllocator?.Dispose();
        _defaultAllocator = null;
    }
}
