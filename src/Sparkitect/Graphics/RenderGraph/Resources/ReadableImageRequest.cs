using JetBrains.Annotations;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Declaration shapes for a <see cref="ReadableImage"/>: a registered shared physical image
/// resolved by <see cref="Identification"/>, an inline transient, or the swapchain backing.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ReadableImageRequest : IResourceRequest<ReadableImage>
{
    public sealed partial record FromRegistered(Identification Id, ReadUsage Usage) : ReadableImageRequest;
    public sealed partial record FromTransient(ImageDescription Description, ReadUsage Usage)
        : ReadableImageRequest;
    public sealed partial record FromSwapchain(ReadUsage Usage) : ReadableImageRequest;
}
