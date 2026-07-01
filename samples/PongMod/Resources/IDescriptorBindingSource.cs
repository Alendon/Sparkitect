using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace PongMod.Resources;

/// <summary>
/// A bindable view: it exposes what the descriptor needs to build a write, but never writes itself.
/// The surface is split by timing. The Execute-time half lives here — <see cref="DescribeBinding"/> is
/// read at push time and returns the payload the descriptor switches on to build the
/// <see cref="WriteDescriptorSet"/>. The Setup-time half is the bound view's <c>static</c>
/// <see cref="DescriptorType"/> metadata (e.g. <c>StorageWriteView.DescriptorType</c>), read off the
/// concrete view type for layout derivation with no live instance. It is deliberately NOT a member of
/// this interface: a static-abstract member would make the interface unusable as the type argument of
/// the covariant <see cref="IGraphResource{T}"/> handle the descriptor iterates (CS8920).
/// </summary>
[PublicAPI]
public interface IDescriptorBindingSource
{
    /// <summary>The Execute-time payload describing the resource to bind; the descriptor turns it into a write.</summary>
    DescriptorBindingPayload DescribeBinding();
}
