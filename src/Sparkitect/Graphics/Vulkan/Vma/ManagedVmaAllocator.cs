using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;
using RawVmaAllocator = Sparkitect.Graphics.Vulkan.Vma.VmaAllocator;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Layer-2 managed VMA allocator. Composes a raw <see cref="VmaAllocator"/> (Layer 1, P/Invoke
/// boundary) and adds per-allocator resource tracking, caller-context capture via
/// <see cref="InjectCallerContextAttribute"/>, and leak reporting at disposal.
/// </summary>
/// <remarks>
/// <para>
/// Per D-08 and D-09: composes (does not inherit) the raw allocator; owns its own
/// <see cref="IObjectTracker{VmaResource}"/>; registers every <see cref="VmaBuffer"/> and
/// <see cref="VmaImage"/> created through this instance.
/// </para>
/// <para>
/// Per D-08-R: on <see cref="Dispose"/> the leaked-resource enumeration is <em>log-only</em>.
/// The allocator does NOT force-destroy leaked resources; after logging it simply calls
/// <c>vmaDestroyAllocator</c>. Diagnostics surface through the leak log + any debug-build
/// VMA warnings. Manual cleanup is the intended consumer path.
/// </para>
/// <para>
/// Leak log format mirrors <see cref="Sparkitect.Graphics.Vulkan.VulkanContext"/>'s
/// <c>Shutdown</c> block verbatim except for the leading warning message — this keeps
/// operator log-parsing consistent across Vulkan and VMA subsystems.
/// </para>
/// </remarks>
public sealed class ManagedVmaAllocator : IDisposable
{
    private readonly IVmaRawOps _rawOps;
    private readonly IObjectTracker<ManagedVmaAllocator>.Handle? _serviceTrackerHandle;

    /// <summary>
    /// Per-allocator resource tracker. Every <see cref="VmaBuffer"/>/<see cref="VmaImage"/>
    /// created through this allocator is registered here; <see cref="Dispose"/> enumerates
    /// and logs any that were not <see cref="VmaResource.Dispose"/>d.
    /// </summary>
    public IObjectTracker<VmaResource> ObjectTracker { get; } = new ObjectTracker<VmaResource>();

    /// <summary>Whether <see cref="Dispose"/> has been called.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Production constructor: builds a raw <see cref="VmaAllocator"/> for the given device
    /// and wraps its public operations in the default <see cref="IVmaRawOps"/> implementation.
    /// </summary>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The Vulkan physical device.</param>
    /// <param name="device">The Vulkan logical device.</param>
    /// <param name="vulkanApiVersion">The API version (caller passes <c>Silk.NET.Vulkan.Vk.Version13</c>).</param>
    /// <param name="serviceTracker">Optional service-level tracker; set when created via <c>IVmaService.CreateAllocator</c>.</param>
    /// <param name="trackingCallsite">Caller-context for service-level tracking.</param>
    internal ManagedVmaAllocator(
        Instance instance,
        PhysicalDevice physicalDevice,
        Device device,
        uint vulkanApiVersion,
        IObjectTracker<ManagedVmaAllocator>? serviceTracker = null,
        CallerContext trackingCallsite = default)
        : this(
            new DefaultVmaRawOps(RawVmaAllocator.Create(instance, physicalDevice, device, vulkanApiVersion)),
            serviceTracker,
            trackingCallsite)
    {
    }

    /// <summary>
    /// Test-seam constructor: accepts a pre-built <see cref="IVmaRawOps"/> so tests can inject
    /// a fake native boundary without spinning up a real <c>VkDevice</c> (per D-21).
    /// </summary>
    internal ManagedVmaAllocator(
        IVmaRawOps rawOps,
        IObjectTracker<ManagedVmaAllocator>? serviceTracker = null,
        CallerContext trackingCallsite = default)
    {
        _rawOps = rawOps;
        if (serviceTracker is not null)
            _serviceTrackerHandle = serviceTracker.Track(this, trackingCallsite);
    }

    /// <summary>
    /// Allocates a VMA-backed Vulkan buffer and returns a tracked <see cref="VmaBuffer"/>.
    /// </summary>
    public VmaBuffer CreateBuffer(
        in BufferCreateInfo bufferInfo,
        in VmaAllocationCreateInfo allocInfo,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        ThrowIfDisposed();
        _rawOps.CreateBuffer(in bufferInfo, in allocInfo,
            out var buffer, out var allocation, out var allocationInfo);
        return new VmaBuffer(this, buffer, allocation, allocationInfo, callerContext);
    }

    /// <summary>
    /// Allocates a VMA-backed Vulkan image and returns a tracked <see cref="VmaImage"/>.
    /// </summary>
    public VmaImage CreateImage(
        in ImageCreateInfo imageInfo,
        in VmaAllocationCreateInfo allocInfo,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        ThrowIfDisposed();
        _rawOps.CreateImage(in imageInfo, in allocInfo,
            out var image, out var allocation, out var allocationInfo);
        return new VmaImage(this, image, allocation, allocationInfo, callerContext);
    }

    internal nint Map(VmaAllocation allocation) => _rawOps.MapMemory(allocation);
    internal void Unmap(VmaAllocation allocation) => _rawOps.UnmapMemory(allocation);
    internal void DestroyBuffer(Buffer buffer, VmaAllocation allocation) => _rawOps.DestroyBuffer(buffer, allocation);
    internal void DestroyImage(Image image, VmaAllocation allocation) => _rawOps.DestroyImage(image, allocation);

    /// <summary>
    /// Disposes the managed allocator. Enumerates the per-allocator
    /// <see cref="IObjectTracker{VmaResource}"/>, logs each leaked resource with its
    /// <see cref="CallerContext"/>, and then disposes the underlying raw allocator.
    /// Per D-08-R, leaked resources are NOT force-destroyed.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;

        var leakedCount = ObjectTracker.Count;
        if (leakedCount > 0)
        {
            Log.Warning("VMA resource leaks detected on allocator dispose: {Count} resource(s) not disposed", leakedCount);
            foreach (var (resource, callsite) in ObjectTracker.GetTrackingEntries())
            {
                Log.Warning("  Leaked {Type} created at {Callsite}",
                    resource.GetType().Name, callsite);
            }
        }

        _rawOps.Dispose();
        _serviceTrackerHandle?.Free();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
}

/// <summary>
/// Abstracted native-operation surface for <see cref="ManagedVmaAllocator"/>. The production
/// implementation wraps the raw <c>VmaAllocator</c> from the VMA project; tests supply a
/// fake to exercise the managed wrapper without spinning up a real Vulkan device (per D-21).
/// </summary>
/// <remarks>
/// Per RESEARCH §"Validation Architecture — Test-project choice" (planner recommendation,
/// delegate-record seam over an <c>IVmaAllocatorFactory</c>-style abstraction), this is a
/// plain interface carrying the six raw operations used by the managed wrapper.
/// Scope is narrow — the interface exists solely to make the wrapper unit-testable; it
/// is NOT a reusable abstraction over VMA.
/// </remarks>
internal interface IVmaRawOps : IDisposable
{
    void CreateBuffer(
        in BufferCreateInfo bufferInfo,
        in VmaAllocationCreateInfo allocInfo,
        out Buffer buffer,
        out VmaAllocation allocation,
        out VmaAllocationInfo allocationInfo);

    void CreateImage(
        in ImageCreateInfo imageInfo,
        in VmaAllocationCreateInfo allocInfo,
        out Image image,
        out VmaAllocation allocation,
        out VmaAllocationInfo allocationInfo);

    nint MapMemory(VmaAllocation allocation);
    void UnmapMemory(VmaAllocation allocation);
    void DestroyBuffer(Buffer buffer, VmaAllocation allocation);
    void DestroyImage(Image image, VmaAllocation allocation);
}

/// <summary>Production <see cref="IVmaRawOps"/> implementation: forwards to the raw <see cref="VmaAllocator"/>.</summary>
internal sealed class DefaultVmaRawOps(RawVmaAllocator raw) : IVmaRawOps
{
    public void CreateBuffer(in BufferCreateInfo bufferInfo, in VmaAllocationCreateInfo allocInfo,
        out Buffer buffer, out VmaAllocation allocation, out VmaAllocationInfo allocationInfo)
        => raw.CreateBuffer(in bufferInfo, in allocInfo, out buffer, out allocation, out allocationInfo);

    public void CreateImage(in ImageCreateInfo imageInfo, in VmaAllocationCreateInfo allocInfo,
        out Image image, out VmaAllocation allocation, out VmaAllocationInfo allocationInfo)
        => raw.CreateImage(in imageInfo, in allocInfo, out image, out allocation, out allocationInfo);

    public nint MapMemory(VmaAllocation allocation) => raw.MapMemory(allocation);
    public void UnmapMemory(VmaAllocation allocation) => raw.UnmapMemory(allocation);
    public void DestroyBuffer(Buffer buffer, VmaAllocation allocation) => raw.DestroyBuffer(buffer, allocation);
    public void DestroyImage(Image image, VmaAllocation allocation) => raw.DestroyImage(image, allocation);
    public void Dispose() => raw.Dispose();
}
