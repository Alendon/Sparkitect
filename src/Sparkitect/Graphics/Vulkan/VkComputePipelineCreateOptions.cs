using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

public sealed record VkComputePipelineCreateOptions(
    VkShaderModule Shader,
    VkPipelineLayout Layout,
    string EntryPoint = "main");
