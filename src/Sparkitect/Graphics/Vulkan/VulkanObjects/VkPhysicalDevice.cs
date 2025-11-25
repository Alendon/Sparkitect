using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPhysicalDevice : VulkanObject
{
    internal VkPhysicalDevice(IVulkanContext vulkanContext, PhysicalDevice physicalDevice)
        : base(vulkanContext)
    {
        this.PhysicalDevice = physicalDevice;
    }

    /// <summary>
    /// The native Vulkan physical device handle.
    /// </summary>
    public PhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    /// Gets the properties of this physical device.
    /// </summary>
    public PhysicalDeviceProperties GetProperties()
    {
        return Vk.GetPhysicalDeviceProperties(PhysicalDevice);
    }

    /// <summary>
    /// Gets the features of this physical device.
    /// </summary>
    public PhysicalDeviceFeatures GetFeatures()
    {
        return Vk.GetPhysicalDeviceFeatures(PhysicalDevice);
    }

    /// <summary>
    /// Gets the memory properties of this physical device.
    /// </summary>
    public PhysicalDeviceMemoryProperties GetMemoryProperties()
    {
        return Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice);
    }

    /// <summary>
    /// Gets the queue family properties of this physical device.
    /// </summary>
    public unsafe QueueFamilyProperties[] GetQueueFamilyProperties()
    {
        uint count = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, ref count, null);
        if (count == 0) return [];

        var properties = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* ptr = properties)
        {
            Vk.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, ref count, ptr);
        }

        return properties;
    }

    /// <summary>
    /// Gets the format properties for a specific format.
    /// </summary>
    public FormatProperties GetFormatProperties(Format format)
    {
        Vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, format, out var properties);
        return properties;
    }

    /// <summary>
    /// Gets the image format properties for a specific configuration.
    /// </summary>
    public Result GetImageFormatProperties(
        Format format,
        ImageType type,
        ImageTiling tiling,
        ImageUsageFlags usage,
        ImageCreateFlags flags,
        out ImageFormatProperties properties)
    {
        return Vk.GetPhysicalDeviceImageFormatProperties(PhysicalDevice, format, type, tiling, usage, flags, out properties);
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        PhysicalDevice = default;
    }
}