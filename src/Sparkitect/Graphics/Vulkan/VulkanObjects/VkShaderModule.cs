using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan shader module compiled from SPIR-V bytecode.</summary>
[PublicAPI]
public class VkShaderModule : VulkanObject
{
    /// <summary>Wraps an existing <see cref="ShaderModule"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkShaderModule(ShaderModule handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="ShaderModule"/> handle.</summary>
    public ShaderModule Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyShaderModule(Device, Handle, AllocationCallbacks);
    }
}
