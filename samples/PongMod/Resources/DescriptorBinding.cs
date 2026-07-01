using JetBrains.Annotations;
using Sparkitect.Graphing;

namespace PongMod.Resources;

/// <summary>One entry in a push descriptor's binding list: a slot, an array index, and the covariant view handle backing it (covariance lets any concrete bindable-view handle be assigned).</summary>
[PublicAPI]
public readonly record struct DescriptorBinding(
    uint Binding,
    uint ArrayIndex,
    IGraphResource<IDescriptorBindingSource> View);
