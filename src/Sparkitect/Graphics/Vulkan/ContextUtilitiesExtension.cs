using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public static class ContextUtilitiesExtension
{
    extension(IVulkanContext vulkanContext)
    {
        /// <summary>
        /// Extend Vulkan context by grouping instance dependent utility functions
        /// </summary>
        /// <remarks> With advancements in the jit compiler, it is able to allocate objects directly on the stack, if they cant escape it. Making this abstraction overhead free </remarks>
        public IVulkanUtilities Utilities => new VulkanUtilities(vulkanContext);
    }
}

// Utility class for various vulkan interactions
// 
internal class VulkanUtilities(IVulkanContext vulkanContext) : IVulkanUtilities
{
    private Vk Api => vulkanContext.VkApi;
    private Device Device => vulkanContext.VkDevice.Handle;
    private Instance Instance => vulkanContext.VkInstance.Handle;
    
    
    


}
