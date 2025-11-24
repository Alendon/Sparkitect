using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkPhysicalDevice : VulkanObject
{
    public PhysicalDevice PhysicalDevice { get; private set; }

    private VkPhysicalDevice(IObjectTracker<VulkanObject> objectTracker, Vk vk, PhysicalDevice physicalDevice) :
        base(objectTracker, vk)
    {
        PhysicalDevice = physicalDevice;
    }

    public static VkPhysicalDevice Create(IVulkanContext vulkanContext)
    {
        var vk = vulkanContext.VkApi;

       
        var devices = vk.GetPhysicalDevices(vulkanContext.VkInstance.Handle);
        if (devices.Count == 0)
        {
            throw new Exception("No physical devices found");
        }

        //TODO Future, add setting to choose which device to use

        var physicalDevice = devices.MaxBy(x => vk.GetPhysicalDeviceProperties(x).DeviceType switch
        {
            PhysicalDeviceType.DiscreteGpu => 3,
            PhysicalDeviceType.IntegratedGpu => 2,
            PhysicalDeviceType.VirtualGpu => 1,
            _ => 0,
        });
        
        if (physicalDevice.Handle == 0)
        {
            throw new Exception("No suitable physical device found");
        }
        
        return new VkPhysicalDevice(vulkanContext.ObjectTracker, vk, physicalDevice);
    }


    public override void Destroy()
    {
        PhysicalDevice = default;
    }
}