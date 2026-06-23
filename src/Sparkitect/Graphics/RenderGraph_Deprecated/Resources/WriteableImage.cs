using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Write-capable view over a graph <see cref="Image"/>. Owns no state of its own —
/// reads/mutates the underlying image. Implements <see cref="IPreExecuteHook"/> to
/// emit the layout transition matching its declared <see cref="WriteUsage"/>.
/// </summary>
/// <remarks>
/// The hook delegates to <see cref="Image.TransitionTo"/>. Mods that need exotic
/// synchronization can either subclass into a sibling view type or read
/// <see cref="UnderlyingImage"/> directly and bypass the helper.
/// </remarks>
[ResourceManager<ImageResourceManager>]
[GraphResourceRegistry.RegisterResource("writeable_image")]
[PublicAPI]
public sealed partial class WriteableImage : IHasIdentification, IPreExecuteHook
{
    private readonly Image _image;
    private readonly WriteUsage _usage;

    internal WriteableImage(Image image, WriteUsage usage)
    {
        _image = image;
        _usage = usage;
    }

    public VkImage VkImage => _image.CurrentVkImage;
    public Extent2D Extent => _image.Extent;
    public Format Format => _image.Format;
    public Image UnderlyingImage => _image;
    public WriteUsage Usage => _usage;

    public void PreExecute(VkCommandBuffer commandBuffer)
    {
        var (layout, access, stage) = Map(_usage);
        _image.TransitionTo(commandBuffer, layout, access, stage);
    }

    internal static (ImageLayout Layout, AccessFlags Access, PipelineStageFlags Stage) Map(WriteUsage usage) => usage switch
    {
        WriteUsage.TransferDst => (
            ImageLayout.TransferDstOptimal,
            AccessFlags.TransferWriteBit,
            PipelineStageFlags.TransferBit),
        WriteUsage.ComputeStorage => (
            ImageLayout.General,
            AccessFlags.ShaderWriteBit,
            PipelineStageFlags.ComputeShaderBit),
        WriteUsage.ColorAttachment => (
            ImageLayout.ColorAttachmentOptimal,
            AccessFlags.ColorAttachmentWriteBit,
            PipelineStageFlags.ColorAttachmentOutputBit),
        _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null),
    };
}
