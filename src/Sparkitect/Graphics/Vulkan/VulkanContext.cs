using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

public class VulkanContext : IVulkanContext
{
    public required Vk VkApi { get; init; }
    public required VkInstance VkInstance { get; init;}
    public required VkPhysicalDevice VkPhysicalDevice { get; init;}
    public required VkDevice VkDevice { get; init;}
    public unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
    public required IObjectTracker<VulkanObject> ObjectTracker { get; init; }
}