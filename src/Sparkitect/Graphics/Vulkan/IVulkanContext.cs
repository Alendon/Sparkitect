using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

public interface IVulkanContext
{
    Vk VkApi { get; }
    VkInstance VkInstance { get; }
    VkPhysicalDevice VkPhysicalDevice { get; }
    VkDevice VkDevice { get; }
    unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
    IObjectTracker<VulkanObject> ObjectTracker { get; }
}