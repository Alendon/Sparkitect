using JetBrains.Annotations;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan semaphore used to synchronize work between GPU queue submissions.</summary>
[PublicAPI]
public class VkSemaphore : VulkanObject
{
    /// <summary>Wraps an existing <see cref="Silk.NET.Vulkan.Semaphore"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkSemaphore(Silk.NET.Vulkan.Semaphore handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="Silk.NET.Vulkan.Semaphore"/> handle.</summary>
    public Silk.NET.Vulkan.Semaphore Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroySemaphore(Device, Handle, AllocationCallbacks);
    }
}
