using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

[StateFacade<IVulkanContextStateFacade>]
public interface IVulkanContext
{
    Vk VkApi { get; }
    VkInstance VkInstance { get; }
    VkPhysicalDevice VkPhysicalDevice { get; }
    VkDevice VkDevice { get; }
    unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
    IObjectTracker<VulkanObject> ObjectTracker { get; }

    /// <summary>
    /// Gets a specific queue by family and index.
    /// </summary>
    /// <returns>The queue, or null if not found.</returns>
    VulkanQueue? GetQueue(uint familyIndex, uint queueIndex);

    /// <summary>
    /// Gets all queues belonging to a queue family.
    /// </summary>
    IReadOnlyList<VulkanQueue> GetQueuesForFamily(uint familyIndex);
}

public interface IVulkanContextStateFacade
{
    void Initialize();
    void CreateInstance();
    void SelectPhysicalDevice();
    void CreateDevice();
    void DestroyDevice();
    void DestroyPhysicalDevice();
    void DestroyInstance();
    void Shutdown();
}