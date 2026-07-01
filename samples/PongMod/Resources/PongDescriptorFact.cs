using System.Collections.Immutable;
using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace PongMod.Resources;

/// <summary>
/// Builds the push-descriptor's live instance. It ctor-injects the graph-local
/// <see cref="IDescriptorLayoutCache"/>; the description does the get-or-create at Declare and flows the
/// derived <see cref="SetLayout"/> + <see cref="Bindings"/> in via a record <c>with</c> (the DI keyed
/// factory constructs the fact without per-declaration data). The layout is cache-owned (D-12), so the
/// descriptor's cleanup is a no-op.
/// </summary>
[FactRegistry.Register("pong_descriptor")]
public sealed partial record PongDescriptorFact(IDescriptorLayoutCache? Cache)
    : DeclaredFact<PongDescriptor>, IHasIdentification
{
    /// <summary>The cache-derived set layout, computed at Declare and set by the description.</summary>
    public VkDescriptorSetLayout SetLayout { get; init; } = null!;

    /// <summary>The ordered bindings the descriptor pushes, set by the description.</summary>
    public ImmutableArray<DescriptorBinding> Bindings { get; init; } = ImmutableArray<DescriptorBinding>.Empty;

    /// <inheritdoc/>
    public PongDescriptor CreateInstance(IInstanceContext ctx) => new(SetLayout, Bindings);

    /// <summary>The set layout is owned by the graph-local cache (D-12); the descriptor releases nothing.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
