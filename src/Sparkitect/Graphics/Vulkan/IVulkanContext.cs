using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.DI.GeneratorAttributes;
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

    VkResult<VkCommandPool> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex, [InjectCallerContext] CallerContext callerContext = default);

    VkResult<VkDescriptorPool> CreateDescriptorPool(in DescriptorPoolCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);

    VkResult<VkSemaphore> CreateSemaphore(SemaphoreCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    VkResult<VkFence> CreateFence(FenceCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    VkResult<VkDescriptorSetLayout> CreateDescriptorSetLayout(in DescriptorSetLayoutCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);
    VkResult<VkPipelineLayout> CreatePipelineLayout(in PipelineLayoutCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);
    VkResult<VkPipeline> CreateComputePipeline(in ComputePipelineCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Creates a Vulkan surface for the given window.
    /// </summary>
    /// <param name="window">The window to create a surface for.</param>
    /// <returns>The surface, or null if surface creation failed.</returns>
    VkSurface? CreateSurface(IWindow window);

    /// <summary>
    /// Gets all queues belonging to a queue family.
    /// </summary>
    IReadOnlyList<VulkanQueue> GetQueuesForFamily(uint familyIndex);
}

[FacadeFor<IVulkanContext>]
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