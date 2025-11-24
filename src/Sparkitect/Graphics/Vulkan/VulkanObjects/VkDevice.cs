using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkDevice : VulkanObject
{
    private readonly unsafe AllocationCallbacks* _allocationCallbacks;

    public Device Handle { get; private set; }

    private unsafe VkDevice(IObjectTracker<VulkanObject> objectTracker, Vk vk, Device vkDevice, AllocationCallbacks* allocationCallbacks)
        : base(objectTracker, vk)
    {
        Handle = vkDevice;
        _allocationCallbacks = allocationCallbacks;
    }

    public override unsafe void Destroy()
    {
        Vk.DestroyDevice(Handle, _allocationCallbacks);
    }
}

public record struct VkDeviceDescription;