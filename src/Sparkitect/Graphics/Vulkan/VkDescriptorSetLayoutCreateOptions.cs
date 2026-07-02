using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkDescriptorSetLayout"/>.</summary>
/// <param name="Bindings">The resource bindings the layout declares.</param>
/// <param name="Flags">Creation flags (e.g. push-descriptor support).</param>
[PublicAPI]
public sealed record VkDescriptorSetLayoutCreateOptions(
    ImmutableArray<DescriptorSetLayoutBinding> Bindings,
    DescriptorSetLayoutCreateFlags Flags = 0);
