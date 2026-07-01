using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the push descriptor over the compute write view (D-10). It binds the write view at
/// (set 0, binding 0), derives the push-descriptor set layout at Declare from the write view's static
/// <see cref="StorageWriteView.DescriptorType"/> via the graph-local cache (D-12), and hands the derived
/// layout + bindings to the fact. The write view handle is intra-pass (by value); the descriptor is
/// Pong-owned and not generalized (D-13).
/// </summary>
[PublicAPI]
public sealed record PongDescriptorDescription(IGraphResource<StorageWriteView> WriteView)
    : IResourceDescription<PongDescriptor>
{
    /// <inheritdoc/>
    public DeclaredFact<PongDescriptor> Declare(IResourceTransaction tx)
    {
        var fact = tx.InstantiateFact<PongDescriptorFact>();

        if (fact.Cache is null)
            throw new InvalidOperationException(
                "PongDescriptorDescription.Declare: no descriptor-layout cache was injected. The " +
                "graph-local IDescriptorLayoutCache must be resolvable when the fact factory builds this fact.");

        // The ordered bindings the descriptor pushes at Execute: binding 0 -> the write view (covariant
        // to IGraphResource<IDescriptorBindingSource>).
        var bindings = ImmutableArray.Create(
            new DescriptorBinding(Binding: 0, ArrayIndex: 0, WriteView));

        // Derive the layout shape from each bound view's static descriptor type + the compute stage, then
        // get-or-create the cache-owned VkDescriptorSetLayout (D-11/D-12).
        var layoutShape = ImmutableArray.Create(
            new DescriptorLayoutBinding(0, StorageWriteView.DescriptorType, ShaderStageFlags.ComputeBit));
        var setLayout = fact.Cache.GetOrCreate(layoutShape);

        return fact with { SetLayout = setLayout, Bindings = bindings };
    }
}
