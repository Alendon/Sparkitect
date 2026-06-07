using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;

namespace PongMod;

/// <summary>
/// Shared-image registrations for PongMod. The compute and copy passes both reference the
/// single registered image by its <c>Identification</c> (cross-pass symbolic reference); the
/// graph materializes one physical backing from this description.
/// </summary>
/// <remarks>
/// The registration value here is a placeholder: the real extent is only known once the
/// mod-owned window exists, so <c>PongRuntimeService</c> re-registers this id imperatively
/// (last-writer-wins) with the live swapchain extent before the graph drains the store.
/// </remarks>
public static class GraphImageRegistrations
{
    /// <summary>Declares the shared render-target id; the extent is supplied imperatively later.</summary>
    [GraphImageRegistry.RegisterSharedImage("target")]
    public static ImageDescription Target() =>
        new(default, Format.R8G8B8A8Unorm, Transient: false, DefaultFill: null);
}
