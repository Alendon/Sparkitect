using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Hooks;

/// <summary>Hook dispatched on a pass's root resources immediately before that pass's <c>Execute</c>.</summary>
[PublicAPI]
public interface IPreExecuteHook
{
    /// <summary>Reconcile the resource's state for its upcoming use, recording any barrier onto <paramref name="commandBuffer"/>.</summary>
    void PreExecute(VkCommandBuffer commandBuffer);
}

/// <summary>Hook dispatched once, after all passes, on the resource that published the finishline moment; home of the present-layout transition.</summary>
[PublicAPI]
public interface IFinishlineHook
{
    /// <summary>Contribute the finishline (present) transition onto <paramref name="commandBuffer"/>. Fires once per frame.</summary>
    void OnFinishline(VkCommandBuffer commandBuffer);
}
