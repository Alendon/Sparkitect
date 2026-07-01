using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// One binding in a descriptor-set layout's shape: the ordered <paramref name="Binding"/> index, its
/// static <paramref name="Type"/>, and the <paramref name="Stages"/> it is visible to.
/// </summary>
[PublicAPI]
public readonly record struct DescriptorLayoutBinding(
    uint Binding,
    DescriptorType Type,
    ShaderStageFlags Stages);

/// <summary>
/// A graph-local get-or-create cache for push-descriptor <see cref="VkDescriptorSetLayout"/>s, keyed by
/// binding shape. Identical shapes share one owned layout, disposed at graph teardown.
/// </summary>
[PublicAPI]
public interface IDescriptorLayoutCache
{
    /// <summary>
    /// Returns the layout for <paramref name="bindings"/>, creating it (with
    /// <see cref="DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr"/>) on first request.
    /// </summary>
    VkDescriptorSetLayout GetOrCreate(ImmutableArray<DescriptorLayoutBinding> bindings);
}
