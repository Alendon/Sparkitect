using Silk.NET.Vulkan;
using Silk.NET.Windowing;
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

    VkResult<VkCommandPool> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex);

    /// <summary>
    /// Creates a Vulkan surface for the given window.
    /// </summary>
    /// <param name="window">The window to create a surface for.</param>
    /// <returns>The surface, or null if surface creation failed.</returns>
    VkSurface? CreateSurface(IWindow window);

    /// <summary>
    /// Creates a shader module from SPIR-V bytecode.
    /// </summary>
    VkResult<VkShaderModule> CreateShaderModule(ReadOnlySpan<byte> spirvCode);

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