using System.Collections.Immutable;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public sealed record VkDescriptorPoolCreateOptions(
    uint MaxSets,
    ImmutableArray<DescriptorPoolSize> PoolSizes,
    DescriptorPoolCreateFlags Flags = 0);
