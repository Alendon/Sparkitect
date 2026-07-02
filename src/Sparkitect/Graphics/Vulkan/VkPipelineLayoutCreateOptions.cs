using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkPipelineLayout"/>.</summary>
/// <param name="SetLayouts">The descriptor set layouts bound by pipelines using this layout.</param>
/// <param name="PushConstantRanges">The push-constant ranges the layout exposes.</param>
[PublicAPI]
public sealed record VkPipelineLayoutCreateOptions(
    ImmutableArray<VkDescriptorSetLayout> SetLayouts,
    ImmutableArray<PushConstantRange> PushConstantRanges);
