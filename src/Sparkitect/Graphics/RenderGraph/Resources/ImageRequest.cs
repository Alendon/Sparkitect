using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declaration shapes for a plain (non-write) <see cref="Image"/>.</summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ImageRequest : IResourceRequest<Image>
{
    public sealed partial record FromSwapchain : ImageRequest;
    public sealed partial record FromTransient(Extent2D Extent, Format Format) : ImageRequest;
}
