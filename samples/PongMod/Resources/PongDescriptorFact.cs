using System.Collections.Immutable;
using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace PongMod.Resources;

/// <summary>Builds the push descriptor's live instance from the cache-derived layout and bindings the description flows in; the <see cref="IDescriptorLayoutCache"/> is ctor-injected.</summary>
[FactRegistry.Register("pong_descriptor")]
public sealed partial record PongDescriptorFact(IDescriptorLayoutCache? Cache)
    : DeclaredFact<PongDescriptor>, IHasIdentification
{
    public VkDescriptorSetLayout SetLayout { get; init; } = null!;

    public ImmutableArray<DescriptorBinding> Bindings { get; init; } = ImmutableArray<DescriptorBinding>.Empty;

    /// <inheritdoc/>
    public PongDescriptor CreateInstance(IInstanceContext ctx) => new(SetLayout, Bindings);

    /// <summary>The set layout is cache-owned, so the descriptor releases nothing.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
