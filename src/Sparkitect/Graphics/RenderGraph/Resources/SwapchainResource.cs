using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// External graph-resource wrapping the engine's swapchain. Published into a render
/// graph via the <see cref="IExternalResourceHandler"/> route (see
/// <c>SwapchainResourceExtensions.Apply</c>); never received through
/// <see cref="ISetupContext.Declare{TResource}"/>. The bound
/// <see cref="ImageResourceManager"/> intentionally does not implement
/// <c>IGraphResourceManagerFor&lt;SwapchainResource&gt;</c>, so a pass attempting
/// to <c>Declare</c> this type fails fast at setup with the standard "no
/// IGraphResourceManagerFor implementation" message.
/// </summary>
[ResourceManager<ImageResourceManager>]
[GraphResourceRegistry.RegisterResource("swapchain")]
[PublicAPI]
public sealed partial class SwapchainResource : IHasIdentification
{
    public VkSwapchain Underlying { get; }
    public Extent2D Extent { get; }
    public Format Format { get; }

    public SwapchainResource(VkSwapchain underlying)
    {
        Underlying = underlying;
        Extent = underlying.Extent;
        Format = underlying.ImageFormat;
    }
}
