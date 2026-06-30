using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Abstract base for compute-category render-graph passes. Authors implement <see cref="Setup"/> to
/// declare the resources the pass uses, and <see cref="Execute"/> to record pass-specific work
/// against the raw command buffer. There are no lifecycle-hook seams.
/// </summary>
[PublicAPI]
public abstract class ComputePass : IPass
{
    /// <summary>Author override — declare graph resource handles via <paramref name="ctx"/>.</summary>
    public abstract void Setup(ISetupContext ctx);

    /// <summary>Author override — record pass-specific work onto <paramref name="commandBuffer"/>.</summary>
    public abstract void Execute(VkCommandBuffer commandBuffer);
}
