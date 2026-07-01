using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// A symbolic image-extent request carried on a description and resolved to a concrete
/// <see cref="Silk.NET.Vulkan.Extent2D"/> at instance time by <see cref="IImageManager"/> — never a baked
/// size. The union is closed: switches over it have no default arm, so a new case is a compile error until handled.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ExtentIntent
{
    /// <summary>Size the image to match the applied swapchain's extent, resolved at instance time.</summary>
    public sealed partial record MatchSwapchain : ExtentIntent;
}
