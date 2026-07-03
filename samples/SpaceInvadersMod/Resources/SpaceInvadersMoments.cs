using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Moments;
using Sparkitect.Graphics.RenderGraph.Push;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;

namespace SpaceInvadersMod.Resources;

/// <summary>SpaceInvadersMod's resource-moment registrations.</summary>
[PublicAPI]
public static class SpaceInvadersMoments
{
    /// <summary>
    /// The <c>entities_raw</c> moment: the externally-pushed CPU entity-list snapshot chain head. Its resource
    /// type is the engine <see cref="PushedResource"/>; the render graph synthesizes its birth increment at
    /// setup because <see cref="EntitiesRawPushMarker"/> marks it externally-pushed.
    /// </summary>
    [ResourceMomentRegistry.RegisterMoment("entities_raw")]
    public static ResourceMomentDefinition EntitiesRaw() => new ResourceMomentDefinition<PushedResource>();

    /// <summary>
    /// The <c>entities_gpu</c> moment: the published device-buffer entity-list composite. The staging pass
    /// publishes it (birth increment marked on <see cref="EntityListResource"/>); the compute pass reads the
    /// same instance — and the element count off it — through this moment.
    /// </summary>
    [ResourceMomentRegistry.RegisterMoment("entities_gpu")]
    public static ResourceMomentDefinition EntitiesGpu() => new ResourceMomentDefinition<EntityListResource>();

    /// <summary>
    /// The <c>target</c> moment: cross-pass identity for the shared compute render target — the compute
    /// pass's storage-write view publishes it, the copy pass's transfer-src read view consumes it.
    /// </summary>
    [ResourceMomentRegistry.RegisterMoment("target")]
    public static ResourceMomentDefinition Target() => new ResourceMomentDefinition<ImageResource>();
}

/// <summary>
/// Marks the <c>entities_raw</c> moment as externally-pushed. The general metadata mechanism keys an
/// <see cref="ExternalPushConfig"/> by this type's <see cref="Identification"/>, which resolves to the
/// <c>entities_raw</c> moment id — so the render graph's setup-time synthesis discovers it among the
/// registered moments and mints its birth increment with no pass authoring the mark.
/// </summary>
// Identified by hand, not through a registry: the [ExternalPush] metadata attribute keys its
// config by this Identification, so IHasIdentification is required but no registry attribute applies.
#pragma warning disable SPARK0262
[ExternalPush]
[PublicAPI]
public sealed class EntitiesRawPushMarker : IHasIdentification
{
    /// <inheritdoc/>
    public static Identification Identification => GraphMomentID.SpaceInvadersMod.EntitiesRaw;
}
#pragma warning restore SPARK0262
