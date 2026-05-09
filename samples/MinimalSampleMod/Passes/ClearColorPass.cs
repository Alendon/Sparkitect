using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.RenderGraph;
using Sparkitect.Windowing;

namespace MinimalSampleMod.Passes;

/// <summary>
/// Walking-skeleton hand-authored render pass (Phase 49 D-D1 / D-B3): a single
/// <c>vkCmdClearColorImage</c> call against the current acquired swapchain image,
/// using <see cref="ImageLayout.TransferDstOptimal"/> to match the hard-coded barrier
/// sequence in <c>RenderGraph.Frame.cs</c>. The color cycles each frame so the manual
/// run produces visible animation.
/// </summary>
/// <remarks>
/// <para>
/// Constructor-injectable with <see cref="IVulkanContext"/>, <see cref="ISparkitWindow"/>,
/// and <see cref="IRenderGraphFrameContext"/>. Plan 03's <c>RenderGraphResolutionProvider</c>
/// supplies <see cref="ISparkitWindow"/> and <see cref="IRenderGraphFrameContext"/> through
/// the <c>IResolutionProvider</c> seam on <c>BuildFactoryContainer</c>; <see cref="IVulkanContext"/>
/// falls through to the host container per <c>ResolutionScope.cs:46-52</c>.
/// </para>
/// <para>
/// Per CONTEXT roadmap-correction (corrected SC#6) and <c>feedback_raw_vulkan_as_signal</c>:
/// raw <c>VkApi.CmdClearColorImage</c> in a pass body is the documented escape hatch at
/// walking-skeleton; heavy raw-Vulkan use across mods would signal an abstraction (e.g.
/// <c>ClearImageView</c>) is warranted, not pre-empted.
/// </para>
/// <para>
/// Per Phase 49 D-D5 the <see cref="Setup"/> body is empty — Phase 54's <c>[GraphResource]</c>
/// SG eventually fills it in for annotated members. The current swapchain image index is
/// read EXCLUSIVELY through <see cref="IRenderGraphFrameContext.CurrentSwapchainImageIndex"/>
/// inside <see cref="Execute"/> (the only legal read window per Plan 03).
/// </para>
/// </remarks>
[RenderPassRegistry.RegisterPass("clear_color")]
internal sealed partial class ClearColorPass : ComputePass
{
    private readonly IVulkanContext _vulkanContext;
    private readonly ISparkitWindow _window;
    private readonly IRenderGraphFrameContext _frameContext;
    private uint _frameCounter;

    public ClearColorPass(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        IRenderGraphFrameContext frameContext)
    {
        _vulkanContext = vulkanContext;
        _window = window;
        _frameContext = frameContext;
    }

    public override void Setup()
    {
        // Empty per Phase 49 D-D5. Phase 54 SG fills this in for [GraphResource]-annotated members.
    }

    public override unsafe void Execute(in ComputePassExecutePayload payload)
    {
        var imageIndex = _frameContext.CurrentSwapchainImageIndex;
        var swapImage = _window.Swapchain.Images[(int)imageIndex];

        // Cycle 0..1 over ~360 frames per channel for a visible animation in manual verification.
        var t = (_frameCounter++ % 360u) / 360f;
        var clearColor = new ClearColorValue
        {
            Float32_0 = t,
            Float32_1 = 1f - t,
            Float32_2 = 0.5f,
            Float32_3 = 1f,
        };

        var range = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            BaseMipLevel = 0, LevelCount = 1,
            BaseArrayLayer = 0, LayerCount = 1,
        };

        // Raw Vulkan — documented escape hatch (corrected SC#6, CONTEXT roadmap-correction
        // + feedback_raw_vulkan_as_signal). ImageLayout MUST be TransferDstOptimal to match
        // the hard-coded barrier emitted by RenderGraph.Frame.cs (Plan 03, D-D9).
        _vulkanContext.VkApi.CmdClearColorImage(
            payload.CommandBuffer.Handle,
            swapImage.Handle,
            ImageLayout.TransferDstOptimal,
            in clearColor,
            1, in range);
    }
}
