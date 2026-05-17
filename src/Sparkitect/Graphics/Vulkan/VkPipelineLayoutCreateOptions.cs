using System.Collections.Immutable;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

public sealed record VkPipelineLayoutCreateOptions(
    ImmutableArray<VkDescriptorSetLayout> SetLayouts,
    ImmutableArray<PushConstantRange> PushConstantRanges);
