using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public sealed record VkComputePipelineCreateOptions(
    VkShaderModule Shader,
    VkPipelineLayout Layout,
    string EntryPoint = "main");
