using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Read-capable view over a graph <see cref="Image"/>. Layout-only: owns no
/// <see cref="VkImageView"/> of its own — it transitions the underlying image to the
/// transfer-source layout matching its declared <see cref="ReadUsage"/> and exposes the
/// raw <see cref="VkImage"/> for transfer ops (e.g. <c>vkCmdBlitImage</c>).
/// </summary>
/// <remarks>
/// The structural twin of <see cref="WriteableImage"/>: transfer-dst stays on the writeable
/// view, transfer-src on this read view. Implements <see cref="IPreExecuteHook"/> to emit the
/// layout transition; mods needing exotic synchronization read <see cref="UnderlyingImage"/>
/// directly and bypass the helper.
/// </remarks>
[ResourceManager<ImageResourceManager>]
[GraphResourceRegistry.RegisterResource("readable_image")]
[PublicAPI]
public sealed partial class ReadableImage : IHasIdentification, IPreExecuteHook
{
    private readonly Image _image;
    private readonly ReadUsage _usage;

    internal ReadableImage(Image image, ReadUsage usage)
    {
        _image = image;
        _usage = usage;
    }

    public VkImage VkImage => _image.CurrentVkImage;
    public Extent2D Extent => _image.Extent;
    public Format Format => _image.Format;
    public Image UnderlyingImage => _image;
    public ReadUsage Usage => _usage;

    public void PreExecute(VkCommandBuffer commandBuffer)
    {
        var (layout, access, stage) = Map(_usage);
        _image.TransitionTo(commandBuffer, layout, access, stage);
    }

    internal static (ImageLayout Layout, AccessFlags Access, PipelineStageFlags Stage) Map(ReadUsage usage) => usage switch
    {
        ReadUsage.TransferSrc => (
            ImageLayout.TransferSrcOptimal,
            AccessFlags.TransferReadBit,
            PipelineStageFlags.TransferBit),
        _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null),
    };
}
