using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// One binding in a descriptor-set layout's shape: the ordered <paramref name="Binding"/> index, its
/// static <paramref name="Type"/>, and the <paramref name="Stages"/> it is visible to. A
/// <c>record struct</c>, so a sequence of these carries value equality — the key material the layout
/// cache dedupes on.
/// </summary>
[PublicAPI]
public readonly record struct DescriptorLayoutBinding(
    uint Binding,
    DescriptorType Type,
    ShaderStageFlags Stages);

/// <summary>
/// A graph-local get-or-create cache for push-descriptor <see cref="VkDescriptorSetLayout"/>s, keyed by
/// binding shape. Identical shapes share one owned layout; the cache disposes every layout it owns at
/// graph teardown. Seed of a future general usage-level-disposable layer — not generalized now (D-12).
/// </summary>
[PublicAPI]
public interface IDescriptorLayoutCache
{
    /// <summary>
    /// Returns the layout for <paramref name="bindings"/>, creating it (with
    /// <see cref="DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr"/>) on first request and returning
    /// the cached instance when the same ordered binding shape recurs.
    /// </summary>
    VkDescriptorSetLayout GetOrCreate(ImmutableArray<DescriptorLayoutBinding> bindings);
}
