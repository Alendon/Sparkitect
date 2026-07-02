using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a window surface (VK_KHR_surface) and queries its swapchain-relevant capabilities.</summary>
[PublicAPI]
public class VkSurface : VulkanObject
{
    private readonly KhrSurface _khrSurface;

    /// <summary>Wraps an existing <see cref="SurfaceKHR"/> handle together with the <see cref="KhrSurface"/> extension.</summary>
    public VkSurface(SurfaceKHR handle, KhrSurface khrSurface, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
        _khrSurface = khrSurface;
    }

    /// <summary>The underlying Silk.NET <see cref="SurfaceKHR"/> handle.</summary>
    public SurfaceKHR Handle { get; }

    /// <summary>Returns the surface capabilities (extents, image-count bounds, transforms) for <paramref name="physicalDevice"/>.</summary>
    public SurfaceCapabilitiesKHR GetCapabilities(PhysicalDevice physicalDevice)
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, Handle, out var capabilities);
        return capabilities;
    }

    /// <summary>Returns the surface formats <paramref name="physicalDevice"/> supports for this surface.</summary>
    public unsafe SurfaceFormatKHR[] GetFormats(PhysicalDevice physicalDevice)
    {
        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, Handle, ref count, null);

        if (count == 0) return [];

        var formats = new SurfaceFormatKHR[count];
        fixed (SurfaceFormatKHR* ptr = formats)
        {
            _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, Handle, ref count, ptr);
        }

        return formats;
    }

    /// <summary>Returns the present modes <paramref name="physicalDevice"/> supports for this surface.</summary>
    public unsafe PresentModeKHR[] GetPresentModes(PhysicalDevice physicalDevice)
    {
        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, Handle, ref count, null);

        if (count == 0) return [];

        var modes = new PresentModeKHR[count];
        fixed (PresentModeKHR* ptr = modes)
        {
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, Handle, ref count, ptr);
        }

        return modes;
    }

    /// <summary>Whether the given queue family on <paramref name="physicalDevice"/> can present to this surface.</summary>
    public bool GetPhysicalDeviceSurfaceSupport(PhysicalDevice physicalDevice, uint queueFamilyIndex)
    {
        _khrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, queueFamilyIndex, Handle, out var supported);
        return supported;
    }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        _khrSurface.DestroySurface(VulkanContext.VkInstance.Handle, Handle, AllocationCallbacks);
    }
}
