using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public sealed record VkDescriptorPoolCreateOptions(
    uint MaxSets,
    ImmutableArray<DescriptorPoolSize> PoolSizes,
    DescriptorPoolCreateFlags Flags = 0);
