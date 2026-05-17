using JetBrains.Annotations;
using System.Collections.Immutable;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public sealed record VkDescriptorSetLayoutCreateOptions(
    ImmutableArray<DescriptorSetLayoutBinding> Bindings,
    DescriptorSetLayoutCreateFlags Flags = 0);
