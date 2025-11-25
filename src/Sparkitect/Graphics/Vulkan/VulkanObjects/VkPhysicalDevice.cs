using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPhysicalDevice : VulkanObject
{
    internal VkPhysicalDevice(IObjectTracker<VulkanObject> objectTracker, Vk vk, PhysicalDevice handle)
        : base(objectTracker, vk)
    {
        Handle = handle;
        objectTracker.Track(this);
    }

    /// <summary>
    /// The native Vulkan physical device handle.
    /// </summary>
    public PhysicalDevice Handle { get; private set; }

    /// <summary>
    /// Gets the properties of this physical device.
    /// </summary>
    public PhysicalDeviceProperties GetProperties()
    {
        return Vk.GetPhysicalDeviceProperties(Handle);
    }

    /// <summary>
    /// Gets the features of this physical device.
    /// </summary>
    public PhysicalDeviceFeatures GetFeatures()
    {
        return Vk.GetPhysicalDeviceFeatures(Handle);
    }

    /// <summary>
    /// Gets the memory properties of this physical device.
    /// </summary>
    public PhysicalDeviceMemoryProperties GetMemoryProperties()
    {
        return Vk.GetPhysicalDeviceMemoryProperties(Handle);
    }

    /// <summary>
    /// Gets the queue family properties of this physical device.
    /// </summary>
    public unsafe QueueFamilyProperties[] GetQueueFamilyProperties()
    {
        uint count = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(Handle, ref count, null);
        if (count == 0) return [];

        var properties = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* ptr = properties)
        {
            Vk.GetPhysicalDeviceQueueFamilyProperties(Handle, ref count, ptr);
        }

        return properties;
    }

    /// <summary>
    /// Gets the format properties for a specific format.
    /// </summary>
    public FormatProperties GetFormatProperties(Format format)
    {
        Vk.GetPhysicalDeviceFormatProperties(Handle, format, out var properties);
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
        return Vk.GetPhysicalDeviceImageFormatProperties(Handle, format, type, tiling, usage, flags, out properties);
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        Handle = default;
    }
}