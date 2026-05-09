using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

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

    Result<VkCommandPool, VkApiResult> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex, [InjectCallerContext] CallerContext callerContext = default);

    Result<VkDescriptorPool, VkApiResult> CreateDescriptorPool(in DescriptorPoolCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);

    Result<VkSemaphore, VkApiResult> CreateSemaphore(SemaphoreCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkFence, VkApiResult> CreateFence(FenceCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkDescriptorSetLayout, VkApiResult> CreateDescriptorSetLayout(in DescriptorSetLayoutCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkPipelineLayout, VkApiResult> CreatePipelineLayout(in PipelineLayoutCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkPipeline, VkApiResult> CreateComputePipeline(in ComputePipelineCreateInfo createInfo, [InjectCallerContext] CallerContext callerContext = default);

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

    /// <summary>
    /// Pre-teardown checkpoint: blocks until the device is idle so all Vulkan-dependent
    /// subsystems can shut down safely. Called by the <c>begin_vulkan_teardown</c>
    /// transition function, which carries no ordering attributes of its own — dependents
    /// reference it via <c>[OrderAfter&lt;BeginVulkanTeardownFunc&gt;]</c> (e.g.
    /// <c>destroy_device</c>, <c>destroy_vma</c>) or <c>[OrderBefore&lt;BeginVulkanTeardownFunc&gt;]</c>
    /// (future render-graph shutdown transitions).
    /// </summary>
    void BeginVulkanTeardown();

    void DestroyDevice();
    void DestroyPhysicalDevice();
    void DestroyInstance();
    void Shutdown();
}