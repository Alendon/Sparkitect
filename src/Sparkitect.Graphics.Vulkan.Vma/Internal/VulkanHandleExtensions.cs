using Silk.NET.Vulkan;
using Vortice.Vulkan;
using VkBuffer = Vortice.Vulkan.VkBuffer;
using VkImage = Vortice.Vulkan.VkImage;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace Sparkitect.Graphics.Vulkan.Vma.Internal;

internal static class VulkanHandleExtensions
{
    // Silk -> Vortice (for passing to VMA)
    internal static VkInstance ToVortice(this Instance silk) => new((nint)silk.Handle);
    internal static VkDevice ToVortice(this Device silk) => new((nint)silk.Handle);
    internal static VkPhysicalDevice ToVortice(this PhysicalDevice silk) => new((nint)silk.Handle);
    internal static VkBuffer ToVortice(this Buffer silk) => new(silk.Handle);
    internal static VkImage ToVortice(this Image silk) => new(silk.Handle);

    // Vortice -> Silk (for returning from VMA)
    internal static Buffer ToSilk(this VkBuffer vortice) => new((ulong)(nuint)vortice.Handle);
    internal static Image ToSilk(this VkImage vortice) => new((ulong)(nuint)vortice.Handle);
    internal static DeviceMemory ToSilk(this VkDeviceMemory vortice) => new((ulong)(nuint)vortice.Handle);
}
