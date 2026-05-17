using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public sealed record VkPipelineLayoutCreateOptions(
    ImmutableArray<VkDescriptorSetLayout> SetLayouts,
    ImmutableArray<PushConstantRange> PushConstantRanges);
