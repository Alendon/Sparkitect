using JetBrains.Annotations;
using Sparkitect.Graphing;

namespace PongMod.Resources;

/// <summary>
/// A single entry in a push descriptor's ordered binding list: a binding slot, an array index within
/// that slot, and the view handle that backs it. The view is held as an
/// <see cref="IGraphResource{T}"/> of <see cref="IDescriptorBindingSource"/> — covariance makes any
/// concrete bindable-view handle (e.g. an <c>IGraphResource&lt;StorageWriteView&gt;</c>) assignable
/// here. The bound view type's static <see cref="IDescriptorBindingSource.DescriptorType"/> drives
/// layout derivation at Setup; its <see cref="IDescriptorBindingSource.DescribeBinding"/> drives the
/// write at Execute.
/// </summary>
[PublicAPI]
public readonly record struct DescriptorBinding(
    uint Binding,
    uint ArrayIndex,
    IGraphResource<IDescriptorBindingSource> View);
