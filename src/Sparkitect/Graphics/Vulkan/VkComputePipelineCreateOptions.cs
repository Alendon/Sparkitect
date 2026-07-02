using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a compute <see cref="VulkanObjects.VkPipeline"/>.</summary>
/// <param name="Shader">The compute shader module.</param>
/// <param name="Layout">The pipeline layout describing its descriptor sets and push constants.</param>
/// <param name="EntryPoint">The shader entry-point function name.</param>
[PublicAPI]
public sealed record VkComputePipelineCreateOptions(
    VkShaderModule Shader,
    VkPipelineLayout Layout,
    string EntryPoint = "main");
