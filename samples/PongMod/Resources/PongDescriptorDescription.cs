using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
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
    private VkDescriptorSetLayout? _setLayout;

    /// <summary>
    /// The derived push-descriptor set layout, produced when <see cref="Declare"/> runs inside the setup
    /// transaction (Setup -> Declare). The owning pass reads it straight off this description immediately
    /// after <c>ctx.Use(this)</c> to build its pipeline layout — no runtime <c>Fetch</c> is needed, since
    /// the layout is frame-independent (cache-owned, D-12) and fully determined by the bound views' static
    /// descriptor types (D-11). This is why the pass-owned pipeline can be assembled entirely at Setup.
    /// </summary>
    public VkDescriptorSetLayout SetLayout =>
        _setLayout ?? throw new InvalidOperationException(
            "PongDescriptorDescription.SetLayout was read before Declare ran — pass this description to " +
            "ctx.Use(...) first; the layout is produced inside the setup transaction.");

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

        // Capture the derived layout so the pass can read it at Setup (see SetLayout). It is frame-
        // independent, so this is the same handle every consumer sees.
        _setLayout = setLayout;

        return fact with { SetLayout = setLayout, Bindings = bindings };
    }
}
