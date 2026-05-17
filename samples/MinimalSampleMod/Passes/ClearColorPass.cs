using System.Diagnostics;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Windowing;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace MinimalSampleMod.Passes;

[RenderPassRegistry.RegisterPass("clear_color")]
internal sealed partial class ClearColorPass : ComputePass
{
    private readonly ISparkitWindow _window;
    private uint _frameCounter;
    private const double MinFrameTimeS = 1 / 120d;
    private long _lastTime;

    public ClearColorPass(ISparkitWindow window)
    {
        _window = window;
    }

    public override void Setup()
    {
        _lastTime = Stopwatch.GetTimestamp();
    }

    public override void Execute(VkCommandBuffer commandBuffer, uint swapchainImageIndex)
    {
        SpinWait.SpinUntil(() =>
            Stopwatch.GetElapsedTime(_lastTime, Stopwatch.GetTimestamp()).TotalSeconds > MinFrameTimeS);
        _lastTime = Stopwatch.GetTimestamp();

        var swapImage = _window.Swapchain.Images[(int)swapchainImageIndex];

        var t = _frameCounter++ % 360u / 360f;
        var clearColor = new ClearColorValue
        {
            Float32_0 = t,
            Float32_1 = 1f - t,
            Float32_2 = 0.5f,
            Float32_3 = 1f,
        };

        commandBuffer.ClearColorImage(swapImage, ImageLayout.TransferDstOptimal, in clearColor);
    }
}
