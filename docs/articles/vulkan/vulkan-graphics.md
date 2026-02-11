---
uid: sparkitect.vulkan.vulkan-graphics
title: Vulkan Graphics System
description: Wrapper types, result handling, CallerContext tracking, and resource lifecycle management
---

# Vulkan Graphics System

Sparkitect wraps Vulkan with C# types that provide type safety, automatic resource tracking, and debugging capabilities while maintaining full access to the Vulkan API.

## IVulkanContext Interface

[`IVulkanContext`](xref:Sparkitect.Graphics.Vulkan.IVulkanContext) is the primary entry point for Vulkan operations. Access it through dependency injection:

```csharp
[StateService<IMyRenderer, MyRenderModule>]
public class MyRenderer : IMyRenderer
{
    public required IVulkanContext VulkanContext { private get; init; }

    public void Initialize()
    {
        // Access raw Vulkan API for advanced operations
        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice;

        // Create Vulkan objects through the context
        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            graphicsQueueFamily);
    }
}
```

Key properties and methods:

**Properties:**

| Member | Type | Description |
|--------|------|-------------|
| `VkApi` | `Vk` | Raw Silk.NET Vulkan API handle |
| `VkInstance` | `VkInstance` | Wrapped Vulkan instance |
| `VkPhysicalDevice` | `VkPhysicalDevice` | Wrapped physical device |
| `VkDevice` | `VkDevice` | Wrapped logical device |
| `DefaultAllocationCallbacks` | `AllocationCallbacks*` | Default Vulkan allocation callbacks (unsafe) |
| `ObjectTracker` | `IObjectTracker<VulkanObject>` | Resource lifecycle tracker |

**Object creation methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateCommandPool()` | `VkResult<VkCommandPool>` | Create a command pool |
| `CreateSemaphore()` | `VkResult<VkSemaphore>` | Create a semaphore |
| `CreateFence()` | `VkResult<VkFence>` | Create a fence |
| `CreateDescriptorPool()` | `VkResult<VkDescriptorPool>` | Create a descriptor pool |
| `CreateDescriptorSetLayout()` | `VkResult<VkDescriptorSetLayout>` | Create a descriptor set layout |
| `CreatePipelineLayout()` | `VkResult<VkPipelineLayout>` | Create a pipeline layout |
| `CreateComputePipeline()` | `VkResult<VkPipeline>` | Create a compute pipeline |
| `CreateSurface()` | `VkSurface?` | Create a Vulkan surface for a window |

**Query methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `GetQueue()` | `VulkanQueue?` | Get a specific queue by family and index |
| `GetQueuesForFamily()` | `IReadOnlyList<VulkanQueue>` | Get all queues belonging to a queue family |

### VulkanQueue Type

`GetQueue()` and `GetQueuesForFamily()` return [`VulkanQueue`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.VulkanQueue) instances. This type wraps a native Vulkan queue with metadata:

| Property | Type | Description |
|----------|------|-------------|
| `Handle` | `Queue` | The native Vulkan queue handle (for API calls) |
| `FamilyIndex` | `uint` | The queue family this queue belongs to |
| `QueueIndex` | `uint` | The index of this queue within its family |
| `Capabilities` | `QueueFlags` | The capability flags of this queue's family (Graphics, Compute, Transfer, etc.) |

```csharp
// Get a graphics queue
var queue = VulkanContext.GetQueue(graphicsQueueFamily, 0);
if (queue is null) throw new InvalidOperationException("Queue not found");

// Use the native handle for Vulkan API calls
var presentQueue = queue.Handle;
```

## VkResult&lt;T&gt; Discriminated Union

Vulkan operations return [`VkResult<T>`](xref:Sparkitect.Graphics.Vulkan.VkResult`1), a discriminated union that forces explicit error handling:

```csharp
[DiscriminatedUnion]
public abstract partial record VkResult<TResultObject>
{
    public sealed record Success(TResultObject value) : VkResult<TResultObject>;
    public sealed record Error(Result errorResult) : VkResult<TResultObject>;
}
```

> **Note**: The `[DiscriminatedUnion]` attribute comes from the `Sundew.DiscriminatedUnions` package, which generates exhaustiveness enforcement for pattern matching.

### Pattern Matching Usage

Use pattern matching to handle success and error cases:

```csharp
var poolResult = VulkanContext.CreateCommandPool(
    CommandPoolCreateFlags.ResetCommandBufferBit,
    queueFamilyIndex);

// Check for error first
if (poolResult is VkResult<VkCommandPool>.Error poolError)
    throw new InvalidOperationException($"Failed to create command pool: {poolError.errorResult}");

// Extract success value
var pool = ((VkResult<VkCommandPool>.Success)poolResult).value;
```

This pattern appears throughout the Pong sample:

```csharp
// Create sync objects
var semaphoreResult = VulkanContext.CreateSemaphore();
if (semaphoreResult is VkResult<VkSemaphore>.Error semaphoreError)
    throw new InvalidOperationException($"Failed to create semaphore: {semaphoreError.errorResult}");
var imageAvailableSemaphore = ((VkResult<VkSemaphore>.Success)semaphoreResult).value;

var fenceResult = VulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
if (fenceResult is VkResult<VkFence>.Error fenceError)
    throw new InvalidOperationException($"Failed to create fence: {fenceError.errorResult}");
var inFlightFence = ((VkResult<VkFence>.Success)fenceResult).value;
```

Alternatively, you can use a switch expression:

```csharp
var pool = poolResult switch
{
    VkResult<VkCommandPool>.Success s => s.value,
    VkResult<VkCommandPool>.Error e => throw new InvalidOperationException($"Failed: {e.errorResult}"),
    _ => throw new InvalidOperationException("Unexpected result type")
};
```

## CallerContext Pattern

Sparkitect tracks where Vulkan objects are created using compile-time location injection, showing exactly where each object originated.

### CallerContext Struct

[`CallerContext`](xref:Sparkitect.Utils.CallerContext) is defined in the `Sparkitect.Utils` namespace:

```csharp
// Sparkitect.Utils namespace
public readonly record struct CallerContext(string FilePath, int LineNumber)
{
    public string FileName => Path.GetFileName(FilePath);
    public override string ToString() => $"{FileName}:{LineNumber}";
}
```

### InjectCallerContext Attribute

Methods that accept `CallerContext` parameters use the [`[InjectCallerContext]`](xref:Sparkitect.Utils.InjectCallerContextAttribute) attribute:

```csharp
VkResult<VkCommandPool> CreateCommandPool(
    CommandPoolCreateFlags flags,
    uint queueFamilyIndex,
    [InjectCallerContext] CallerContext callerContext = default);
```

The source generator intercepts calls to these methods and automatically injects the actual file path and line number at compile time. You never need to pass this parameter manually - it's injected automatically:

```csharp
// You write this:
var pool = VulkanContext.CreateCommandPool(flags, queueFamily);

// Generator transforms it to (conceptually):
var pool = VulkanContext.CreateCommandPool(flags, queueFamily,
    new CallerContext("MyRenderer.cs", 42));
```

### Debugging Benefits

When debugging resource leaks, the object tracker can report exactly where each object was created:

```csharp
// Dump all tracked objects with their creation locations
VulkanContext.ObjectTracker.DumpToLog("Checking for leaks");

// Output example (exact format depends on configured Serilog sink):
// ObjectTracker[Checking for leaks]: 3 objects tracked
// ObjectTracker[Checking for leaks]:   VkCommandPool from PongRuntimeService.cs:72
// ObjectTracker[Checking for leaks]:   VkSemaphore from PongRuntimeService.cs:85
// ObjectTracker[Checking for leaks]:   VkFence from PongRuntimeService.cs:95
```

> **Note**: `DumpToLog` uses Serilog structured logging internally. The exact output format depends on your configured Serilog sink. The example above is illustrative.

## Wrapper Type Naming Convention

Sparkitect uses a `Vk` prefix for all managed Vulkan wrapper types to distinguish them from raw Silk.NET types:

| Wrapper Type | Silk.NET Type | Purpose |
|--------------|---------------|---------|
| `VkInstance` | `Instance` | Wrapped Vulkan instance |
| `VkDevice` | `Device` | Wrapped logical device |
| `VkPhysicalDevice` | `PhysicalDevice` | Wrapped physical device |
| `VkCommandPool` | `CommandPool` | Managed command pool |
| `VkCommandBuffer` | `CommandBuffer` | Managed command buffer |
| `VkSemaphore` | `Semaphore` | Managed semaphore |
| `VkFence` | `Fence` | Managed fence |
| `VkSwapchain` | `SwapchainKHR` | Managed swapchain |
| `VkSurface` | `SurfaceKHR` | Managed window surface |
| `VkPipeline` | `Pipeline` | Managed pipeline |
| `VkDescriptorPool` | `DescriptorPool` | Managed descriptor pool |
| `VkDescriptorSet` | `DescriptorSet` | Managed descriptor set |

Wrapper types provide:
- Automatic resource tracking via `IObjectTracker`
- Proper `IDisposable` implementation
- CallerContext for debugging
- Access to the underlying handle via `.Handle` property

## ObjectTracker

The [`IObjectTracker<T>`](xref:Sparkitect.Utils.IObjectTracker`1) interface tracks resource lifetimes to detect leaks and monitor allocations. On `IVulkanContext`, the concrete type is `IObjectTracker<VulkanObject>`:

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

### Automatic Tracking

Vulkan wrapper objects are automatically tracked when created through `IVulkanContext`:

```csharp
// Object is automatically tracked with caller location
var pool = VulkanContext.CreateCommandPool(flags, queueFamily);

// When disposed, object is automatically untracked
pool.Dispose();
```

### Manual Inspection

Check for resource leaks by inspecting tracked objects:

```csharp
// Get count of tracked objects
var count = VulkanContext.ObjectTracker.Count;

// Get all tracked objects with their creation locations
foreach (var (obj, callsite) in VulkanContext.ObjectTracker.GetTrackingEntries())
{
    Log.Debug("Tracked: {Type} from {Location}", obj.GetType().Name, callsite);
}

// Dump to log for debugging
VulkanContext.ObjectTracker.DumpToLog("Before cleanup");
```

## Resource Cleanup

Wrapper objects implement `IDisposable`. Always dispose Vulkan resources when done:

```csharp
public void Cleanup()
{
    // Wait for GPU to finish all operations
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

**Best practices:**
- Always call `DeviceWaitIdle()` before cleanup to ensure GPU operations complete
- Dispose resources in reverse creation order
- Null-check with `?.Dispose()` pattern for optional resources
- Command pools automatically free their allocated command buffers on disposal

## See Also

- <xref:sparkitect.vulkan.shader-compilation> for the Slang shader workflow and pipeline creation
- <xref:sparkitect.windowing.windowing-input> for window surfaces and input handling
- <xref:sparkitect.core.dependency-injection> for accessing `IVulkanContext` through DI
- `samples/PongMod/` for a complete rendering example
