using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

namespace PongMod;

/// <summary>
/// Shared-image registrations for PongMod. The compute and copy passes both reference the
/// single registered image by its <c>Identification</c> (cross-pass symbolic reference); the
/// graph materializes one physical backing from this description.
/// </summary>
public static class GraphImageRegistrations
{
    /// <summary>
    /// Declares the shared render-target image sized to the live swapchain extent. The mod-owned
    /// window is created during state setup, before the render-graph registries are processed, so
    /// the runtime service's swapchain is available to size the image here.
    /// </summary>
    [GraphImageRegistry.RegisterSharedImage("target")]
    public static ImageDescription Target(IPongRuntimeService pong) =>
        new(pong.Window.Swapchain.Extent, Format.R8G8B8A8Unorm, Transient: false, DefaultFill: null);
}
