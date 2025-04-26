using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public class VkPhysicalDevice : VkObject
{
    private IVkEngine _vkEngine;
    public PhysicalDevice PhysicalDevice { get; private set; }


    private VkPhysicalDevice(AllocationHandler allocationHandler, IVkEngine vkEngine, PhysicalDevice physicalDevice) :
        base(allocationHandler, vkEngine.VkApi)
    {
        _vkEngine = vkEngine;
        PhysicalDevice = physicalDevice;
    }

    public static VkPhysicalDevice Create(IVkEngine vkEngine, AllocationHandler allocationHandler)
    {
        var vk = vkEngine.VkApi;

        var devices = vk.GetPhysicalDevices(vkEngine.Instance.Handle);
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
        
        return new VkPhysicalDevice(allocationHandler, vkEngine, physicalDevice);
    }


    public override void Destroy()
    {
        PhysicalDevice = default;
    }
}