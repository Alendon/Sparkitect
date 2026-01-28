using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkInstance : VulkanObject
{
    // Custom allocation callbacks for Vulkan. Null uses default system allocator.
    private readonly unsafe AllocationCallbacks* _allocationCallbacks = null;

    internal unsafe VkInstance(Instance handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public Instance Handle { get; }

    /// <summary>
    /// Enumerates all physical devices available to this instance.
    /// </summary>
    public PhysicalDevice[] EnumeratePhysicalDevices()
    {
        return Vk.GetPhysicalDevices(Handle).ToArray();
    }

    /// <summary>
    /// Gets the properties of a physical device.
    /// </summary>
    public PhysicalDeviceProperties GetPhysicalDeviceProperties(PhysicalDevice physicalDevice)
    {
        return Vk.GetPhysicalDeviceProperties(physicalDevice);
    }

    /// <summary>
    /// Gets the features of a physical device.
    /// </summary>
    public PhysicalDeviceFeatures GetPhysicalDeviceFeatures(PhysicalDevice physicalDevice)
    {
        return Vk.GetPhysicalDeviceFeatures(physicalDevice);
    }

    /// <summary>
    /// Gets the queue family properties of a physical device.
    /// </summary>
    public unsafe QueueFamilyProperties[] GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice physicalDevice)
    {
        uint count = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref count, null);

        if (count == 0) return [];

        var properties = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* ptr = properties)
        {
            Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref count, ptr);
        }

        return properties;
    }

    /// <summary>
    /// Gets the memory properties of a physical device.
    /// </summary>
    public PhysicalDeviceMemoryProperties GetPhysicalDeviceMemoryProperties(PhysicalDevice physicalDevice)
    {
        return Vk.GetPhysicalDeviceMemoryProperties(physicalDevice);
    }

    /// <summary>
    /// Gets the address of an instance-level Vulkan function by name.
    /// </summary>
    public unsafe nint GetInstanceProcAddr(string name)
    {
        return (nint)Vk.GetInstanceProcAddr(Handle, name);
    }

    /// <summary>
    /// Checks if an instance extension is enabled.
    /// </summary>
    public bool IsInstanceExtensionPresent(string extensionName)
    {
        return Vk.IsInstanceExtensionPresent(extensionName);
    }

    /// <inheritdoc />
    public override unsafe void Destroy()
    {
        Vk.DestroyInstance(Handle, _allocationCallbacks);
    }
}