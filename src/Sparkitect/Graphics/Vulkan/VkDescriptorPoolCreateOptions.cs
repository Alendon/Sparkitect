using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkDescriptorPool"/>.</summary>
/// <param name="MaxSets">Maximum number of descriptor sets allocatable from the pool.</param>
/// <param name="PoolSizes">Per-descriptor-type capacity of the pool.</param>
/// <param name="Flags">Creation flags (e.g. free-descriptor-set support).</param>
[PublicAPI]
public sealed record VkDescriptorPoolCreateOptions(
    uint MaxSets,
    ImmutableArray<DescriptorPoolSize> PoolSizes,
    DescriptorPoolCreateFlags Flags = 0);
