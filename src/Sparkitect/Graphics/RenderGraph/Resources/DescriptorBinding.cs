using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphing;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>A binding value backed by a graph-resource handle: it declares the binding's <see cref="DescriptorType"/> (read at Declare for layout derivation) and, at Execute, fetches the bound view and delegates to it for the write payload. The handle is fetched per Execute so the payload never captures a stale frame instance; the covariant <see cref="IGraphResource{T}"/> lets any concrete bindable-view handle be assigned.</summary>
[PublicAPI]
public sealed record DescriptorBinding(DescriptorType DescriptorType, IGraphResource<IDescriptorValue> View)
    : IDescriptorValue
{
    /// <inheritdoc/>
    public DescriptorBindingPayload DescribeBinding() => View.Fetch().DescribeBinding();
}
