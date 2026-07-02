using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan pipeline holding the compiled shader and fixed-function state for a draw or dispatch.</summary>
[PublicAPI]
public class VkPipeline : VulkanObject
{
    /// <summary>Wraps an existing <see cref="Pipeline"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkPipeline(Pipeline handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="Pipeline"/> handle.</summary>
    public Pipeline Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyPipeline(Device, Handle, AllocationCallbacks);
    }
}
