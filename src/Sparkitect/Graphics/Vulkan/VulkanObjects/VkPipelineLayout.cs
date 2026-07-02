using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan pipeline layout describing the descriptor sets and push constants a pipeline binds.</summary>
[PublicAPI]
public class VkPipelineLayout : VulkanObject
{
    /// <summary>Wraps an existing <see cref="PipelineLayout"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkPipelineLayout(PipelineLayout handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="PipelineLayout"/> handle.</summary>
    public PipelineLayout Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyPipelineLayout(Device, Handle, AllocationCallbacks);
    }
}
