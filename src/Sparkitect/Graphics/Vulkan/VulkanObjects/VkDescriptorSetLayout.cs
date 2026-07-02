using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan descriptor set layout describing the resource bindings a descriptor set exposes.</summary>
[PublicAPI]
public class VkDescriptorSetLayout : VulkanObject
{
    /// <summary>Wraps an existing <see cref="DescriptorSetLayout"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkDescriptorSetLayout(DescriptorSetLayout handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="DescriptorSetLayout"/> handle.</summary>
    public DescriptorSetLayout Handle { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyDescriptorSetLayout(Device, Handle, AllocationCallbacks);
    }
}
