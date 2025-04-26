using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public class VkEngine : IVkEngine
{
    public required Vk VkApi { get; init; }
    public required VkInstance Instance { get; init;}
    public required VkPhysicalDevice PhysicalDevice { get; init;}
    public required VkDevice Device { get; init;}
    public unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
}