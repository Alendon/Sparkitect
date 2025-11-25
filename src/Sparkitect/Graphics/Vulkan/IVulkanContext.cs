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
    /// Gets the primary graphics queue. Always available after device creation.
    /// </summary>
    VulkanQueue GraphicsQueue { get; }

    /// <summary>
    /// Gets a dedicated compute queue if available, otherwise null.
    /// Falls back to graphics queue via <see cref="GraphicsQueue"/> if needed.
    /// </summary>
    VulkanQueue? ComputeQueue { get; }

    /// <summary>
    /// Gets a dedicated transfer queue if available, otherwise null.
    /// Falls back to graphics queue via <see cref="GraphicsQueue"/> if needed.
    /// </summary>
    VulkanQueue? TransferQueue { get; }

    /// <summary>
    /// Gets all queues belonging to a specific queue family.
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