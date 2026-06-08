using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;

namespace SpaceInvadersMod;

/// <summary>
/// Shared graph-resource registrations for SpaceInvaders. The compute and copy passes reference
/// the shared target image by its <c>Identification</c>; the staging and compute passes reference
/// the shared entity buffer the same way. The graph materializes one physical backing per
/// registration.
/// </summary>
public static class GraphRegistrations
{
    /// <summary>
    /// Declares the shared render-target image sized to the live swapchain extent. The mod-owned
    /// window is created during state setup, before the render-graph registries are processed, so
    /// the runtime service's swapchain is available to size the image here.
    /// </summary>
    [GraphImageRegistry.RegisterSharedImage("target")]
    public static ImageDescription Target(ISpaceInvadersRuntimeService si) =>
        new(si.Window.Swapchain.Extent, Format.R8G8B8A8Unorm, Transient: false, DefaultFill: null);

    /// <summary>
    /// Declares the shared device entity buffer, sized for <see cref="SpaceInvadersConstants.MaxRenderEntities"/>
    /// <see cref="RenderEntity"/> elements (the manager grows this on demand).
    /// </summary>
    [GraphBufferRegistry.RegisterSharedBuffer("entities")]
    public static BufferDescription Entities() =>
        new(
            ElementStride: (ulong)Marshal.SizeOf<RenderEntity>(),
            InitialCapacity: SpaceInvadersConstants.MaxRenderEntities);
}
