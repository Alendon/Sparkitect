using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace PongMod.Resources;

/// <summary>A bindable view: exposes what the descriptor needs to build a write, but never writes itself. The Setup-time half (the bound view's <c>static DescriptorType</c>) is deliberately not a member here — a static-abstract member would break use as the covariant <see cref="IGraphResource{T}"/> type argument the descriptor iterates (CS8920).</summary>
[PublicAPI]
public interface IDescriptorBindingSource
{
    DescriptorBindingPayload DescribeBinding();
}
