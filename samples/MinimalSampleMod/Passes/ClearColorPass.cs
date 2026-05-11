using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.RenderGraph;
using Sparkitect.Windowing;

namespace MinimalSampleMod.Passes;

/// <summary>
/// Hand-authored render pass that clears the current swapchain image to a cycling
/// color via <c>vkCmdClearColorImage</c>. The image layout must be
/// <see cref="ImageLayout.TransferDstOptimal"/> — the surrounding render graph emits
/// the matching barriers.
/// </summary>
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
    }

    public override unsafe void Execute(in ComputePassExecutePayload payload)
    {
        var imageIndex = _frameContext.CurrentSwapchainImageIndex;
        var swapImage = _window.Swapchain.Images[(int)imageIndex];

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

        _vulkanContext.VkApi.CmdClearColorImage(
            payload.CommandBuffer.Handle,
            swapImage.Handle,
            ImageLayout.TransferDstOptimal,
            in clearColor,
            1, in range);
    }
}
