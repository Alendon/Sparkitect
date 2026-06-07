using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Declaration shapes for a <see cref="WriteableImage"/>: swapchain-backed, transient, or a
/// registered shared physical image resolved by <see cref="Identification"/>.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record WriteableImageRequest : IResourceRequest<WriteableImage>
{
    public sealed partial record FromSwapchain(WriteUsage Usage) : WriteableImageRequest;
    public sealed partial record FromTransient(Extent2D Extent, Format Format, WriteUsage Usage)
        : WriteableImageRequest;
    public sealed partial record FromRegistered(Identification Id, WriteUsage Usage)
        : WriteableImageRequest;
}
