using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Stock graph image. Holds frame-aliased physical backings and the tracked layout /
/// access / queue-family state for each backing. State is public — views are expected
/// to read and (when bypassing the supplied helpers) write it directly to keep
/// custom synchronization paths coherent.
/// </summary>
[ResourceManager<ImageResourceManager>]
[GraphResourceRegistry.RegisterResource("image")]
[PublicAPI]
public sealed partial class Image : IHasIdentification
{
    private readonly VkImage[] _backings;
    private readonly ImageLayout[] _layouts;
    private readonly AccessFlags[] _accesses;
    private readonly uint[] _queueFamilies;
    private int _currentIndex;

    public Extent2D Extent { get; }
    public Format Format { get; }
    public VkImage CurrentVkImage => _backings[_currentIndex];

    public ImageLayout CurrentLayout
    {
        get => _layouts[_currentIndex];
        set => _layouts[_currentIndex] = value;
    }
    public AccessFlags CurrentAccess
    {
        get => _accesses[_currentIndex];
        set => _accesses[_currentIndex] = value;
    }
    public uint CurrentQueueFamily
    {
        get => _queueFamilies[_currentIndex];
        set => _queueFamilies[_currentIndex] = value;
    }

    internal int BackingCount => _backings.Length;
    internal int CurrentBackingIndex => _currentIndex;
    internal void SetCurrentIndex(int idx) => _currentIndex = idx;

    public Image(VkImage[] backings, Extent2D extent, Format format, uint initialQueueFamily)
    {
        _backings = backings;
        _layouts = new ImageLayout[backings.Length];
        _accesses = new AccessFlags[backings.Length];
        _queueFamilies = new uint[backings.Length];
        for (var i = 0; i < backings.Length; i++) _queueFamilies[i] = initialQueueFamily;
        Extent = extent;
        Format = format;
    }

    /// <summary>
    /// Emit a layout transition for the currently-bound backing and update tracked state.
    /// No-op if the image is already in <paramref name="newLayout"/>. Views that need
    /// finer-grained barrier control (subresource ranges, dependency flags, queue family
    /// transfer) should issue their own <c>cmd.ImageBarrier(...)</c> and write
    /// <see cref="CurrentLayout"/> / <see cref="CurrentAccess"/> directly.
    /// </summary>
    public void TransitionTo(
        VkCommandBuffer commandBuffer,
        ImageLayout newLayout,
        AccessFlags newAccess,
        PipelineStageFlags dstStage)
    {
        if (CurrentLayout == newLayout) return;

        var srcAccess = CurrentAccess;
        var srcStage = srcAccess == 0 ? PipelineStageFlags.TopOfPipeBit : DeriveStage(srcAccess);

        commandBuffer.ImageBarrier(CurrentVkImage,
            oldLayout: CurrentLayout,
            newLayout: newLayout,
            srcStage: srcStage,
            dstStage: dstStage,
            srcAccess: srcAccess,
            dstAccess: newAccess);

        CurrentLayout = newLayout;
        CurrentAccess = newAccess;
    }

    private static PipelineStageFlags DeriveStage(AccessFlags access) => access switch
    {
        AccessFlags.TransferWriteBit => PipelineStageFlags.TransferBit,
        AccessFlags.ShaderWriteBit => PipelineStageFlags.ComputeShaderBit,
        AccessFlags.ColorAttachmentWriteBit => PipelineStageFlags.ColorAttachmentOutputBit,
        _ => PipelineStageFlags.TopOfPipeBit,
    };
}
