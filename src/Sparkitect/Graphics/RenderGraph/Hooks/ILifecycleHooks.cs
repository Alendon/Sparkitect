using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Hooks;

/// <summary>
/// Pre-execute lifecycle hook the render graph dispatches on a pass's plan-derived root resources
/// immediately before that pass's <c>Execute</c>. An implementing root resource reconciles its own
/// carried runtime state against the state its use requires — computing and recording the resulting
/// barrier/layout transition onto <paramref name="commandBuffer"/> — and may cascade to any
/// sub-resources it owns. The graph holds no knowledge of any resource's runtime state: it only
/// type-casts each root to this interface and dispatches at the plan-positioned point. A pass never
/// invokes the hook itself.
/// </summary>
[PublicAPI]
public interface IPreExecuteHook
{
    /// <summary>
    /// Reconcile the resource's state for its upcoming use, recording any barrier/transition onto
    /// <paramref name="commandBuffer"/>. Carries the frame's recording command buffer as the
    /// reconciliation context; the resource supplies the rest from its own carried state.
    /// </summary>
    void PreExecute(VkCommandBuffer commandBuffer);
}

/// <summary>
/// Finishline-position lifecycle hook the render graph dispatches once, after ALL passes have executed,
/// on the root resource that published the finishline moment. It is the home of the present-layout
/// transition (to <see cref="Silk.NET.Vulkan.ImageLayout.PresentSrcKhr"/>), which must fire after the
/// final pass (e.g. after a copy/blit) rather than in any per-pass pre-execute slot. The graph issues
/// no present transition itself — it dispatches this hook and then asserts present readiness.
/// </summary>
[PublicAPI]
public interface IFinishlineHook
{
    /// <summary>
    /// Contribute the finishline (present) transition onto <paramref name="commandBuffer"/> after every
    /// pass has recorded its work. Fires exactly once per frame, on the finishline-publishing resource.
    /// </summary>
    void OnFinishline(VkCommandBuffer commandBuffer);
}
