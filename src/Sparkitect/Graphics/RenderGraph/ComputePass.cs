using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Abstract base for compute-category render-graph passes. Authors implement
/// <see cref="Setup"/> and <see cref="Execute"/>; the base routes hook-interface
/// invocations through a slot composition seam before invoking the author method.
/// </summary>
public abstract class ComputePass : IPass, ISetupHook, IExecuteHook
{
    void ISetupHook.Setup()
    {
        InvokeSlotSetupHooks();
        Setup();
    }

    void IExecuteHook.Execute(VkCommandBuffer commandBuffer, uint swapchainImageIndex)
    {
        InvokeSlotExecuteHooks(commandBuffer, swapchainImageIndex);
        Execute(commandBuffer, swapchainImageIndex);
    }

    /// <summary>Author override — declare graph resource handles. Empty body is permitted.</summary>
    public abstract void Setup();

    /// <summary>Author override — record pass-specific work.</summary>
    public abstract void Execute(VkCommandBuffer commandBuffer, uint swapchainImageIndex);

    /// <summary>Slot-level Setup composition seam. Default no-op; later generators emit a partial override.</summary>
    protected virtual void InvokeSlotSetupHooks() { }

    /// <summary>Slot-level Execute composition seam. Default no-op; later generators emit a partial override.</summary>
    protected virtual void InvokeSlotExecuteHooks(VkCommandBuffer commandBuffer, uint swapchainImageIndex) { }
}
