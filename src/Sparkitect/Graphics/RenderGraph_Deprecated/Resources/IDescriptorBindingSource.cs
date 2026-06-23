using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// A bindable view: it exposes what the descriptor needs to build a write, but never writes itself.
/// The surface is split by timing — <see cref="DescriptorType"/> is static metadata read at Setup
/// (for layout derivation, no frame object touched), while <see cref="DescribeBinding"/> is read at
/// Execute (push time) and returns the payload the descriptor switches on to build the
/// <see cref="WriteDescriptorSet"/>.
/// </summary>
[PublicAPI]
public interface IDescriptorBindingSource
{
    /// <summary>Static descriptor type of this binding source, read at Setup for layout derivation.</summary>
    DescriptorType DescriptorType { get; }

    /// <summary>The Execute-time payload describing the resource to bind; the descriptor turns it into a write.</summary>
    DescriptorBindingPayload DescribeBinding();
}
