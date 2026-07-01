using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>Abstract base for compute-category render-graph passes.</summary>
[PublicAPI]
public abstract class ComputePass : IPass, IDisposable
{
    /// <summary>Declare the resources this pass uses via <paramref name="ctx"/>.</summary>
    public abstract void Setup(ISetupContext ctx);

    /// <summary>Record pass-specific work onto <paramref name="commandBuffer"/>.</summary>
    public abstract void Execute(VkCommandBuffer commandBuffer);

    /// <summary>Destroy any GPU objects the pass owns. Called at graph teardown after the device is idle.</summary>
    public virtual void Dispose() { }
}
