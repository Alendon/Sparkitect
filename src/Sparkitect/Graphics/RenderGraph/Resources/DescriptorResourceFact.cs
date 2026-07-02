using System.Collections.Immutable;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Builds the push descriptor's live instance from the cache-derived layout and the binding values the description flows in; the <see cref="IDescriptorLayoutCache"/> is ctor-injected (graph-local).</summary>
[FactRegistry.Register("descriptor")]
public sealed partial record DescriptorResourceFact(IDescriptorLayoutCache? Cache)
    : DeclaredFact<DescriptorResource>, IHasIdentification
{
    /// <summary>The cache-derived push-descriptor set layout, flowed in by the description at Declare.</summary>
    public VkDescriptorSetLayout SetLayout { get; init; } = null!;

    /// <summary>The ordered binding values the descriptor pushes; slot = position.</summary>
    public ImmutableArray<IDescriptorValue> Bindings { get; init; } = ImmutableArray<IDescriptorValue>.Empty;

    /// <inheritdoc/>
    public DescriptorResource CreateInstance(IInstanceContext ctx) => new(SetLayout, Bindings);

    /// <summary>The set layout is cache-owned, so the descriptor releases nothing.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
