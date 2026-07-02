using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>The general engine push-descriptor description and the single funnel through which passes interact with descriptors: it takes the binding values that build up the set, derives the combined <see cref="VkDescriptorSetLayout"/> at Declare via the graph-local cache (exposed as the <see cref="SetLayout"/> declaration product the pass reads for its pipeline layout), and pushes one set at Execute. One set, <c>firstSet:0</c>, <c>VK_KHR_push_descriptor</c>. Slots are assigned by value position; all bindings are compute-visible. pNext / exotic layouts are out of scope — a specialized description handles those.</summary>
[PublicAPI]
public sealed class DescriptorResourceDescription : IResourceDescription<DescriptorResource>
{
    private readonly ImmutableArray<IDescriptorValue> _descriptors;
    private VkDescriptorSetLayout? _setLayout;

    /// <summary>Builds a push-descriptor description over <paramref name="descriptors"/>; each value's slot is its position in the span.</summary>
    public DescriptorResourceDescription(params ReadOnlySpan<IDescriptorValue> descriptors)
    {
        _descriptors = ImmutableArray.Create(descriptors);
    }

    /// <summary>The derived set layout, produced when <see cref="Declare"/> runs inside the setup transaction; frame-independent, so the owning pass reads it straight off this description at Setup with no runtime <c>Fetch</c>.</summary>
    public VkDescriptorSetLayout SetLayout =>
        _setLayout ?? throw new InvalidOperationException(
            "DescriptorResourceDescription.SetLayout was read before Declare ran — pass this description to " +
            "ctx.Use(...) first; the layout is produced inside the setup transaction.");

    /// <inheritdoc/>
    public DeclaredFact<DescriptorResource> Declare(IResourceTransaction tx)
    {
        var fact = tx.InstantiateFact<DescriptorResourceFact>();

        if (fact.Cache is null)
            throw new InvalidOperationException(
                "DescriptorResourceDescription.Declare: no descriptor-layout cache was injected. The " +
                "graph-local IDescriptorLayoutCache must be resolvable when the fact factory builds this fact.");

        // Derive the layout shape from each binding value's descriptor type; slot = position in the span.
        var layoutShape = ImmutableArray.CreateBuilder<DescriptorLayoutBinding>(_descriptors.Length);
        for (var i = 0; i < _descriptors.Length; i++)
            layoutShape.Add(new DescriptorLayoutBinding((uint)i, _descriptors[i].DescriptorType, ShaderStageFlags.ComputeBit));

        var setLayout = fact.Cache.GetOrCreate(layoutShape.ToImmutable());

        // Frame-independent, so every consumer sees this same handle.
        _setLayout = setLayout;

        return fact with { SetLayout = setLayout, Bindings = _descriptors };
    }
}
