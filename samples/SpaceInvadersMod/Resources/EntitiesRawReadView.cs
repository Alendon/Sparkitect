using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Push;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Read-usage view over the externally-pushed <c>entities_raw</c> snapshot: resolves the frame's bound
/// <see cref="PushedResource"/> (the CPU entity list the ECS published through the external-push door) and
/// exposes it as a typed <see cref="GpuRenderEntity"/> span the staging pass memcpys into its host buffer.
/// The count is the span length; no manager and no DI reach into the ECS.
/// </summary>
[PublicAPI]
public sealed class EntitiesRawReadView
{
    private readonly PushedResource _pushed;

    /// <summary>Composes the read view over the frame's bound pushed snapshot.</summary>
    public EntitiesRawReadView(PushedResource pushed) => _pushed = pushed;

    /// <summary>The pushed snapshot reinterpreted as render entities; empty until the first ECS publish.</summary>
    public ReadOnlySpan<GpuRenderEntity> Entities =>
        MemoryMarshal.Cast<byte, GpuRenderEntity>(_pushed.Data.Span);
}
