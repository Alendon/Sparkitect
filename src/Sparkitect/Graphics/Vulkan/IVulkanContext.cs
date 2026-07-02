using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using JetBrains.Annotations;
using Silk.NET.Windowing;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan;

[StateFacade<IVulkanContextStateFacade>]
[PublicAPI]
public interface IVulkanContext
{
    Vk VkApi { get; }
    VkInstance VkInstance { get; }
    VkPhysicalDevice VkPhysicalDevice { get; }
    VkDevice VkDevice { get; }
    VmaAllocator VmaAllocator { get; }
    unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
    IObjectTracker<VulkanObject> ObjectTracker { get; }

    /// <summary>
    /// The loaded <c>VK_KHR_push_descriptor</c> device-extension handle, used to push
    /// descriptor-set writes inline into a command buffer without a pool-allocated set.
    /// </summary>
    KhrPushDescriptor KhrPushDescriptor { get; }

    /// <summary>
    /// Throws <see cref="VulkanValidationException"/> if the validation layer captured an ERROR since the
    /// last drain. Called at chokepoints so a validation defect surfaces unswallowed at the offending call.
    /// </summary>
    void ThrowIfPendingValidationError();

    /// <summary>
    /// Gets a specific queue by family and index.
    /// </summary>
    /// <returns>The queue, or null if not found.</returns>
    VkQueue? GetQueue(uint familyIndex, uint queueIndex);

    Result<VkCommandPool, VkApiResult> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex, [InjectCallerContext] CallerContext callerContext = default);

    Result<VkDescriptorPool, VkApiResult> CreateDescriptorPool(VkDescriptorPoolCreateOptions options, [InjectCallerContext] CallerContext callerContext = default);

    Result<VkSemaphore, VkApiResult> CreateSemaphore(SemaphoreCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkFence, VkApiResult> CreateFence(FenceCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkDescriptorSetLayout, VkApiResult> CreateDescriptorSetLayout(VkDescriptorSetLayoutCreateOptions options, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkPipelineLayout, VkApiResult> CreatePipelineLayout(VkPipelineLayoutCreateOptions options, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkPipeline, VkApiResult> CreateComputePipeline(VkComputePipelineCreateOptions options, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkSampler, VkApiResult> CreateSampler(VkSamplerCreateOptions options, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkShaderModule, VkApiResult> CreateShaderModule(ReadOnlySpan<uint> spirvCode, [InjectCallerContext] CallerContext callerContext = default);

    Result<VkImage, VkApiResult> CreateImage(VkImageCreateOptions options, in VmaAllocationCreateInfo allocInfo, [InjectCallerContext] CallerContext callerContext = default);
    Result<VkBuffer, VkApiResult> CreateBuffer(VkBufferCreateOptions options, in VmaAllocationCreateInfo allocInfo, [InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Creates a 2D storage image (typical compute-shader target) with VMA-backed memory.
    /// </summary>
    Result<VkImage, VkApiResult> CreateStorageImage2D(
        Extent2D extent,
        Format format,
        VmaMemoryUsage memoryUsage = VmaMemoryUsage.GpuOnly,
        ImageUsageFlags extraUsage = ImageUsageFlags.TransferSrcBit,
        [InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Creates a persistently-mapped storage buffer (CPU-to-GPU upload target) of the given size.
    /// </summary>
    Result<VkBuffer, VkApiResult> CreateMappedStorageBuffer(
        ulong size,
        [InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Creates a device-local storage buffer usable as a transfer-copy destination.
    /// </summary>
    Result<VkBuffer, VkApiResult> CreateDeviceStorageBuffer(
        ulong size,
        [InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Creates a Vulkan surface for the given window.
    /// </summary>
    /// <param name="window">The window to create a surface for.</param>
    /// <returns>The surface, or null if surface creation failed.</returns>
    VkSurface? CreateSurface(IWindow window);

    /// <summary>
    /// Gets all queues belonging to a queue family.
    /// </summary>
    IReadOnlyList<VkQueue> GetQueuesForFamily(uint familyIndex);
}

[FacadeFor<IVulkanContext>]
[PublicAPI]
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
    /// <c>destroy_device</c>) or <c>[OrderBefore&lt;BeginVulkanTeardownFunc&gt;]</c>
    /// (future render-graph shutdown transitions).
    /// </summary>
    void BeginVulkanTeardown();

    void DestroyDevice();
    void DestroyPhysicalDevice();
    void DestroyInstance();
    void Shutdown();
}