using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace PongMod.Resources;

/// <summary>Declares the push descriptor over the compute write view: binds it at (set 0, binding 0) and derives the set layout at Declare from the view's static <see cref="StorageWriteView.DescriptorType"/> via the graph-local cache.</summary>
[PublicAPI]
public sealed record PongDescriptorDescription(IGraphResource<StorageWriteView> WriteView)
    : IResourceDescription<PongDescriptor>
{
    private VkDescriptorSetLayout? _setLayout;

    /// <summary>The derived set layout, produced when <see cref="Declare"/> runs inside the setup transaction; frame-independent, so the owning pass reads it straight off this description at Setup with no runtime <c>Fetch</c>.</summary>
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

        // Binding 0 -> the write view (covariant to IGraphResource<IDescriptorBindingSource>).
        var bindings = ImmutableArray.Create(
            new DescriptorBinding(Binding: 0, ArrayIndex: 0, WriteView));

        // Derive the layout shape from the bound view's static descriptor type, then get-or-create the
        // cache-owned VkDescriptorSetLayout.
        var layoutShape = ImmutableArray.Create(
            new DescriptorLayoutBinding(0, StorageWriteView.DescriptorType, ShaderStageFlags.ComputeBit));
        var setLayout = fact.Cache.GetOrCreate(layoutShape);

        // Frame-independent, so every consumer sees this same handle.
        _setLayout = setLayout;

        return fact with { SetLayout = setLayout, Bindings = bindings };
    }
}
