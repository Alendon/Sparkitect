using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan sampler describing how shaders read from an image.</summary>
[PublicAPI]
public class VkSampler : VulkanObject
{
    /// <summary>Wraps an existing <see cref="Sampler"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkSampler(Sampler handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="Sampler"/> handle.</summary>
    public Sampler Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroySampler(Device, Handle, AllocationCallbacks);
    }
}
