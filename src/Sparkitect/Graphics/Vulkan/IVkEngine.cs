using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public interface IVkEngine
{
    Vk VkApi { get; }
    VkInstance Instance { get; }
    VkPhysicalDevice PhysicalDevice { get; }
    VkDevice Device { get; }
    unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
}