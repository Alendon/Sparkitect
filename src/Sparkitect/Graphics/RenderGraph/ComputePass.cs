using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Abstract base for compute-category render-graph passes. Authors implement
/// <see cref="Setup"/> and <see cref="Execute"/>; the base routes hook-interface
/// invocations through a slot composition seam before invoking the author method.
/// </summary>
[PublicAPI]
public abstract class ComputePass : IPass, ISetupHook, IExecuteHook
{
    void ISetupHook.Setup(ISetupContext ctx)
    {
        InvokeSlotSetupHooks();
        Setup(ctx);
    }

    void IExecuteHook.Execute(VkCommandBuffer commandBuffer)
    {
        InvokeSlotPreExecuteHooks(commandBuffer);
        Execute(commandBuffer);
    }

    /// <summary>Author override — declare graph resource handles via <paramref name="ctx"/>.</summary>
    public abstract void Setup(ISetupContext ctx);

    /// <summary>Author override — record pass-specific work.</summary>
    public abstract void Execute(VkCommandBuffer commandBuffer);

    /// <summary>Slot-level Setup composition seam. Default no-op; later generators emit a partial override.</summary>
    protected virtual void InvokeSlotSetupHooks() { }

    /// <summary>Slot-level PreExecute composition seam. Default no-op; later generators emit a partial override that fires each slot view's <see cref="Hooks.IPreExecuteHook"/>.</summary>
    protected virtual void InvokeSlotPreExecuteHooks(VkCommandBuffer commandBuffer) { }
}
