using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declaration shapes for a <see cref="WriteableImage"/>: swapchain-backed or transient.</summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record WriteableImageRequest : IResourceRequest<WriteableImage>
{
    public sealed partial record FromSwapchain(WriteUsage Usage) : WriteableImageRequest;
    public sealed partial record FromTransient(Extent2D Extent, Format Format, WriteUsage Usage)
        : WriteableImageRequest;
}
