using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// A single entry in a <see cref="DescriptorRequest"/>: a binding slot, an array index within
/// that slot, and the view handle that backs it. The view is held as an
/// <see cref="IGraphResource{T}"/> of <see cref="IDescriptorBindingSource"/> — covariance makes
/// any concrete bindable-view handle (e.g. an <c>IGraphResource&lt;StorageImageView&gt;</c>)
/// assignable here. The view's static <see cref="IDescriptorBindingSource.DescriptorType"/> drives
/// layout derivation at Setup; its <see cref="IDescriptorBindingSource.DescribeBinding"/> drives the
/// write at Execute.
/// </summary>
[PublicAPI]
public readonly record struct DescriptorBinding(
    uint Binding,
    uint ArrayIndex,
    IGraphResource<IDescriptorBindingSource> View);

/// <summary>
/// Declaration shape for a push-descriptor <see cref="Descriptor"/>: an ordered list of
/// <see cref="DescriptorBinding"/>s composed into a single descriptor set (always
/// <c>firstSet:0</c>) plus the shader stages the bindings are visible to. No pool, no
/// allocated set — the descriptor pushes its writes inline at Execute.
/// </summary>
[PublicAPI]
public readonly record struct DescriptorRequest(
    ImmutableArray<DescriptorBinding> Bindings,
    ShaderStageFlags Stages = ShaderStageFlags.ComputeBit) : IResourceRequest<Descriptor>;
