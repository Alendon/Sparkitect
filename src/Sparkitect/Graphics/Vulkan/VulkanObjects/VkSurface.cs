using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkSurface : VulkanObject
{
    private readonly KhrSurface _khrSurface;

    internal VkSurface(SurfaceKHR handle, KhrSurface khrSurface, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
        _khrSurface = khrSurface;
    }

    public SurfaceKHR Handle { get; }

    public unsafe SurfaceCapabilitiesKHR GetCapabilities(PhysicalDevice physicalDevice)
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, Handle, out var capabilities);
        return capabilities;
    }

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

    public unsafe bool GetPhysicalDeviceSurfaceSupport(PhysicalDevice physicalDevice, uint queueFamilyIndex)
    {
        _khrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, queueFamilyIndex, Handle, out var supported);
        return supported;
    }

    public override unsafe void Destroy()
    {
        _khrSurface.DestroySurface(VulkanContext.VkInstance.Handle, Handle, AllocationCallbacks);
    }
}
