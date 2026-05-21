using Silk.NET.Vulkan;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declaration shapes for a plain (non-write) <see cref="Image"/>.</summary>
[DiscriminatedUnion]
internal abstract partial record ImageRequest : IResourceRequest<Image>
{
    internal sealed partial record FromSwapchain : ImageRequest;
    internal sealed partial record FromTransient(Extent2D Extent, Format Format) : ImageRequest;
}
