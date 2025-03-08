using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public class VkDevice
{
    public Device InternalDevice { get; }
    
    public VkDevice(Device device)
    {
        InternalDevice = device;
        
        Vk a;
        DeviceCreateInfo b = new()
        {
            SType = StructureType.DeviceCreateInfo,
            
        };
    }
    
}

public record struct VkDeviceDescription;