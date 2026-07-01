using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>A descriptor binding value: its <see cref="DescriptorType"/> is read at Declare for layout derivation, and <see cref="DescribeBinding"/> yields the Execute-time payload the descriptor turns into a write. The binding slot is assigned by position in the description's value span. Kept a plain interface — <see cref="DescriptorType"/> is an instance member, never static-abstract — so an implementer may wrap a covariant <see cref="Sparkitect.Graphing.IGraphResource{T}"/> handle without tripping CS8920.</summary>
[PublicAPI]
public interface IDescriptorValue
{
    /// <summary>The descriptor type, read at Setup for layout derivation (no frame object touched).</summary>
    DescriptorType DescriptorType { get; }

    /// <summary>The Execute-time payload the descriptor switches on to build the write.</summary>
    DescriptorBindingPayload DescribeBinding();
}
