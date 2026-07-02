---
uid: sparkitect.vulkan.vulkan-graphics
title: Vulkan Graphics System
description: Wrapper types, result handling, CallerContext tracking, and resource lifecycle management
---

# Vulkan Graphics System

Sparkitect wraps Vulkan with C# types that add type safety, automatic resource tracking, and debugging while keeping full access to the raw Silk.NET API.

## IVulkanContext Interface

[`IVulkanContext`](xref:Sparkitect.Graphics.Vulkan.IVulkanContext) is the primary entry point for Vulkan operations. Access it through dependency injection:

```csharp
[StateService<IMyRenderer, MyRenderModule>]
public class MyRenderer : IMyRenderer
{
    public required IVulkanContext VulkanContext { private get; init; }

    public void Initialize()
    {
        var vk = VulkanContext.VkApi;          // raw Silk.NET API for advanced operations
        var device = VulkanContext.VkDevice;

        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            graphicsQueueFamily);
    }
}
```

**Properties:**

| Member | Type | Description |
|--------|------|-------------|
| `VkApi` | `Vk` | Raw Silk.NET Vulkan API handle |
| `VkInstance` | `VkInstance` | Wrapped Vulkan instance |
| `VkPhysicalDevice` | `VkPhysicalDevice` | Wrapped physical device |
| `VkDevice` | `VkDevice` | Wrapped logical device |
| `VmaAllocator` | `VmaAllocator` | VMA allocator backing image and buffer memory |
| `DefaultAllocationCallbacks` | `AllocationCallbacks*` | Allocation callbacks passed to every native create/destroy call (unsafe) |
| `ObjectTracker` | `IObjectTracker<VulkanObject>` | Resource lifecycle tracker |
| `KhrPushDescriptor` | `KhrPushDescriptor` | Loaded `VK_KHR_push_descriptor` device-extension handle |

**Object creation methods:**

Every `Create*` method returns [`Result<T, VkApiResult>`](xref:Sparkitect.Utils.DU.Result`2), where `VkApiResult` is Silk.NET's `Result` enum. Structured creation takes an options record; convenience overloads cover the common compute cases.

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateCommandPool(flags, queueFamilyIndex)` | `Result<VkCommandPool, VkApiResult>` | Command pool for a queue family |
| `CreateSemaphore(flags = 0)` | `Result<VkSemaphore, VkApiResult>` | Semaphore |
| `CreateFence(flags = 0)` | `Result<VkFence, VkApiResult>` | Fence |
| `CreateDescriptorPool(VkDescriptorPoolCreateOptions)` | `Result<VkDescriptorPool, VkApiResult>` | Descriptor pool |
| `CreateDescriptorSetLayout(VkDescriptorSetLayoutCreateOptions)` | `Result<VkDescriptorSetLayout, VkApiResult>` | Descriptor set layout |
| `CreatePipelineLayout(VkPipelineLayoutCreateOptions)` | `Result<VkPipelineLayout, VkApiResult>` | Pipeline layout |
| `CreateComputePipeline(VkComputePipelineCreateOptions)` | `Result<VkPipeline, VkApiResult>` | Compute pipeline |
| `CreateSampler(VkSamplerCreateOptions)` | `Result<VkSampler, VkApiResult>` | Sampler |
| `CreateShaderModule(ReadOnlySpan<uint> spirv)` | `Result<VkShaderModule, VkApiResult>` | Shader module from SPIR-V |
| `CreateImage(VkImageCreateOptions, in VmaAllocationCreateInfo)` | `Result<VkImage, VkApiResult>` | VMA-backed image |
| `CreateBuffer(VkBufferCreateOptions, in VmaAllocationCreateInfo)` | `Result<VkBuffer, VkApiResult>` | VMA-backed buffer |
| `CreateStorageImage2D(extent, format, ...)` | `Result<VkImage, VkApiResult>` | 2D storage image (compute target) |
| `CreateMappedStorageBuffer(size)` | `Result<VkBuffer, VkApiResult>` | Persistently-mapped upload buffer |
| `CreateDeviceStorageBuffer(size)` | `Result<VkBuffer, VkApiResult>` | Device-local transfer-copy target |
| `CreateSurface(IWindow window)` | `VkSurface?` | Window surface (null on failure) |

**Query methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `GetQueue(familyIndex, queueIndex)` | `VkQueue?` | A specific queue, or null if not found |
| `GetQueuesForFamily(familyIndex)` | `IReadOnlyList<VkQueue>` | All queues in a family |

The context also exposes `ThrowIfPendingValidationError()`, called at chokepoints so a captured validation-layer error surfaces at the offending call instead of being swallowed.

### VkQueue Type

`GetQueue()` and `GetQueuesForFamily()` return [`VkQueue`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.VkQueue), a [`VulkanObject`](xref:Sparkitect.Graphics.Vulkan.VulkanObject) that wraps a native queue with metadata:

| Property | Type | Description |
|----------|------|-------------|
| `Handle` | `Queue` | Native Vulkan queue handle |
| `FamilyIndex` | `uint` | Queue family this queue belongs to |
| `QueueIndex` | `uint` | Index within the family |
| `Capabilities` | `QueueFlags` | Capability flags of the queue's family |

Queues are owned by the device; `VkQueue.Destroy()` is a no-op. `Submit` overloads submit a command buffer with or without wait/signal semaphores and a fence:

```csharp
var queue = VulkanContext.GetQueue(graphicsQueueFamily, 0);
if (queue is null) throw new InvalidOperationException("Queue not found");

queue.Submit(commandBuffer);  // no semaphores, no fence
```

## Result&lt;T, VkApiResult&gt; Handling

Vulkan operations return [`Result<TOk, TError>`](xref:Sparkitect.Utils.DU.Result`2) from `Sparkitect.Utils.DU`, the engine's two-arm result type. `Create*` methods specialize it as `Result<T, VkApiResult>`, forcing explicit error handling:

```csharp
[DiscriminatedUnion]
public abstract partial record Result<TOk, TError>
{
    public static implicit operator Result<TOk, TError>(TOk value) => new Ok(value);
    public static implicit operator Result<TOk, TError>(TError value) => new Error(value);

    public sealed partial record Ok(TOk Value) : Result<TOk, TError>;
    public sealed partial record Error(TError Value) : Result<TOk, TError>;
}
```

The `[DiscriminatedUnion]` attribute comes from the `Sundew.DiscriminatedUnions` package, which enforces exhaustive pattern matching.

### Pattern Matching Usage

Check for the error arm first, then take the success payload:

```csharp
var poolResult = VulkanContext.CreateCommandPool(
    CommandPoolCreateFlags.ResetCommandBufferBit,
    queueFamilyIndex);

if (poolResult is Result<VkCommandPool, VkApiResult>.Error poolError)
    throw new InvalidOperationException($"Failed to create command pool: {poolError.Value}");

var pool = ((Result<VkCommandPool, VkApiResult>.Ok)poolResult).Value;
```

A switch expression handles both arms at once:

```csharp
var pool = poolResult switch
{
    Result<VkCommandPool, VkApiResult>.Ok ok => ok.Value,
    Result<VkCommandPool, VkApiResult>.Error e =>
        throw new InvalidOperationException($"Failed: {e.Value}"),
};
```

The implicit conversions let a `Create*` implementation return a bare handle or a bare `VkApiResult` and have it lifted into the correct arm.

## Images and Buffers

[`VkImage`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.VkImage) and [`VkBuffer`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.VkBuffer) wrap VMA-allocated GPU memory. Create them from an options record plus a `VmaAllocationCreateInfo`:

```csharp
var imageResult = VulkanContext.CreateImage(
    new VkImageCreateOptions(
        Extent: new Extent3D(width, height, 1),
        Format: Format.R8G8B8A8Unorm,
        Usage: ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit),
    new VmaAllocationCreateInfo { usage = VmaMemoryUsage.GpuOnly });
```

`VkImage.Backing` is an [`ImageBacking`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.ImageBacking) discriminated union: a `Swapchain` image is owned by its swapchain and not destroyed with the wrapper, while a `VmaAllocated` image frees its allocation on disposal. `VkImage.CreateView()` builds a matching `VkImageView`, inferring the aspect mask and view type from the image's format and layers.

`VkBuffer` records its `Size`, `Usage`, and a `MappedData` pointer that is non-zero when the allocation was created mapped.

For the common compute cases, skip the options records and use the convenience overloads:

| Overload | Produces |
|----------|----------|
| `CreateStorageImage2D(extent, format, memoryUsage, extraUsage)` | GPU-only 2D storage image, transfer-src by default |
| `CreateMappedStorageBuffer(size)` | Persistently-mapped CPU-to-GPU upload buffer |
| `CreateDeviceStorageBuffer(size)` | Device-local buffer usable as a transfer-copy destination |

## CallerContext Pattern

Sparkitect records where each Vulkan object is created using compile-time location injection.

### CallerContext Struct

[`CallerContext`](xref:Sparkitect.Utils.CallerContext) is defined in the `Sparkitect.Utils` namespace:

```csharp
public readonly record struct CallerContext(string FilePath, int LineNumber)
{
    public string FileName => Path.GetFileName(FilePath);
    public override string ToString() => $"{FileName}:{LineNumber}";
}
```

### InjectCallerContext Attribute

Every `Create*` method takes a trailing `CallerContext` parameter marked with [`[InjectCallerContext]`](xref:Sparkitect.Utils.InjectCallerContextAttribute):

```csharp
Result<VkCommandPool, VkApiResult> CreateCommandPool(
    CommandPoolCreateFlags flags,
    uint queueFamilyIndex,
    [InjectCallerContext] CallerContext callerContext = default);
```

The source generator intercepts calls and injects the file path and line number at compile time. You never pass this parameter:

```csharp
// You write this:
var pool = VulkanContext.CreateCommandPool(flags, queueFamily);

// Generator transforms it to (conceptually):
var pool = VulkanContext.CreateCommandPool(flags, queueFamily,
    new CallerContext("MyRenderer.cs", 42));
```

### Debugging Benefits

When chasing a resource leak, the object tracker reports exactly where each live object was created:

```csharp
VulkanContext.ObjectTracker.DumpToLog("Checking for leaks");

// Output example (exact format depends on the configured Serilog sink):
// ObjectTracker[Checking for leaks]: 3 objects tracked
// ObjectTracker[Checking for leaks]:   VkCommandPool from PongRuntimeService.cs:72
// ObjectTracker[Checking for leaks]:   VkSemaphore from PongRuntimeService.cs:85
// ObjectTracker[Checking for leaks]:   VkFence from PongRuntimeService.cs:95
```

## Wrapper Type Naming Convention

Managed wrappers use a `Vk` prefix to distinguish them from raw Silk.NET types:

| Wrapper Type | Silk.NET Type | Purpose |
|--------------|---------------|---------|
| `VkInstance` | `Instance` | Vulkan instance |
| `VkDevice` | `Device` | Logical device |
| `VkPhysicalDevice` | `PhysicalDevice` | Physical device |
| `VkCommandPool` | `CommandPool` | Command pool |
| `VkCommandBuffer` | `CommandBuffer` | Command buffer |
| `VkSemaphore` | `Semaphore` | Semaphore |
| `VkFence` | `Fence` | Fence |
| `VkQueue` | `Queue` | Device queue |
| `VkSwapchain` | `SwapchainKHR` | Swapchain |
| `VkSurface` | `SurfaceKHR` | Window surface |
| `VkPipeline` | `Pipeline` | Pipeline |
| `VkPipelineLayout` | `PipelineLayout` | Pipeline layout |
| `VkDescriptorPool` | `DescriptorPool` | Descriptor pool |
| `VkDescriptorSet` | `DescriptorSet` | Descriptor set |
| `VkDescriptorSetLayout` | `DescriptorSetLayout` | Descriptor set layout |
| `VkImage` | `Image` | VMA-backed image |
| `VkImageView` | `ImageView` | Image view |
| `VkBuffer` | `Buffer` | VMA-backed buffer |
| `VkSampler` | `Sampler` | Sampler |
| `VkShaderModule` | `ShaderModule` | Shader module |

Wrapper types share a common base, [`VulkanObject`](xref:Sparkitect.Graphics.Vulkan.VulkanObject), which gives them:

- Automatic tracking via `IObjectTracker`
- `IDisposable` with a type-specific `Destroy()`
- A recorded `CallerContext` for debugging
- Access to the underlying handle via `.Handle`

## ObjectTracker

The [`IObjectTracker<T>`](xref:Sparkitect.Utils.IObjectTracker`1) interface tracks resource lifetimes to detect leaks. On `IVulkanContext`, the concrete type is `IObjectTracker<VulkanObject>`:

```csharp
public interface IObjectTracker<T>
{
    Handle Track(T obj);
    Handle Track(T obj, CallerContext callsite);
    void Untrack(T obj);
    ICollection<T> GetTracked();
    IEnumerable<(T Object, CallerContext Callsite)> GetTrackingEntries();
    void DumpToLog(string context = "");
    int Count { get; }
}
```

Wrapper objects are tracked automatically when created through `IVulkanContext` and untracked on disposal:

```csharp
var pool = VulkanContext.CreateCommandPool(flags, queueFamily);
// ... use it ...
pool.Dispose();  // untracked here
```

Inspect the tracker to find leaks:

```csharp
var count = VulkanContext.ObjectTracker.Count;

foreach (var (obj, callsite) in VulkanContext.ObjectTracker.GetTrackingEntries())
    Log.Debug("Tracked: {Type} from {Location}", obj.GetType().Name, callsite);
```

## Resource Cleanup

Wrapper objects implement `IDisposable`. Dispose Vulkan resources when done:

```csharp
public void Cleanup()
{
    VulkanContext.VkApi.DeviceWaitIdle(VulkanContext.VkDevice.Handle);

    // Dispose in reverse creation order (dependencies first)
    _descriptorPool?.Dispose();
    _computePipeline?.Dispose();
    _pipelineLayout?.Dispose();
    _descriptorSetLayout?.Dispose();

    _inFlightFence?.Dispose();
    _renderFinishedSemaphore?.Dispose();
    _imageAvailableSemaphore?.Dispose();

    _commandPool?.Dispose();
    _window?.Dispose();
}
```

- Call `DeviceWaitIdle()` before cleanup so GPU operations complete first
- Dispose in reverse creation order
- Null-check with `?.Dispose()` for optional resources
- Command pools free their command buffers on disposal; VMA-backed images and buffers free their allocation on disposal

## See Also

- <xref:sparkitect.vulkan.shader-compilation> for the Slang shader workflow and pipeline creation
- <xref:sparkitect.windowing.windowing-input> for window surfaces and input handling
- <xref:sparkitect.core.dependency-injection> for accessing `IVulkanContext` through DI
- `samples/PongMod/` for a complete rendering example
