using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Wrapper around a Vulkan logical device.
/// </summary>
[PublicAPI]
public class VkDevice : VulkanObject
{
    internal unsafe VkDevice(
        IVulkanContext vulkanContext, Device device)
        : base(vulkanContext)
    {
        Handle = device;
    }

    /// <summary>
    /// Gets the native Vulkan device handle.
    /// </summary>
    public Device Handle { get; }

    /// <summary>
    /// Gets a queue from the device.
    /// </summary>
    /// <param name="queueFamilyIndex">The queue family index.</param>
    /// <param name="queueIndex">The queue index within the family.</param>
    /// <returns>The queue handle.</returns>
    public Queue GetQueue(uint queueFamilyIndex, uint queueIndex)
    {
        Vk.GetDeviceQueue(Handle, queueFamilyIndex, queueIndex, out var queue);
        return queue;
    }

    /// <summary>
    /// Waits for the device to become idle.
    /// </summary>
    public void WaitIdle()
    {
        var result = Vk.DeviceWaitIdle(Handle);
        if (result != Result.Success)
            throw new InvalidOperationException($"Failed to wait for device idle: {result}");
    }

    /// <summary>
    /// Gets the address of a device-level Vulkan function by name.
    /// </summary>
    public unsafe nint GetDeviceProcAddr(string name)
    {
        return (nint)Vk.GetDeviceProcAddr(Handle, name);
    }

    /// <inheritdoc />
    public override unsafe void Destroy()
    {
        Vk.DestroyDevice(Handle, AllocationCallbacks);
    }
}
