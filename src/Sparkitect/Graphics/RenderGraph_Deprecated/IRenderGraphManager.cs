using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Consolidated read surface for the render-graph subsystem: pass catalog,
/// resource-manager-type catalog, render-graph-type catalog, plus the construction
/// entry point <see cref="CreateGraph{TRenderGraph}"/>.
/// </summary>
[RegistryFacade<IRenderGraphManagerRegistryFacade>]
[StateFacade<IRenderGraphManagerStateFacade>]
[PublicAPI]
public interface IRenderGraphManager
{
    /// <summary>All pass identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredPassIds { get; }

    /// <summary>All graph-resource identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredResourceIds { get; }

    /// <summary>All render-graph-type identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredRenderGraphIds { get; }

    /// <summary>Returns the manager type bound to <paramref name="resourceId"/>; throws if unregistered.</summary>
    Type GetManagerTypeFor(Identification resourceId);

    /// <summary>Non-throwing variant of <see cref="GetManagerTypeFor"/>.</summary>
    bool TryGetManagerType(Identification resourceId, out Type managerType);

    /// <summary>Non-throwing lookup of a registered render-graph type.</summary>
    bool TryGetRenderGraphType(Identification renderGraphId, out Type renderGraphType);

    /// <summary>
    /// Builds and returns a ready-to-run render graph of type <typeparamref name="TRenderGraph"/>
    /// wired with the supplied <paramref name="passIds"/> and target <paramref name="window"/>.
    /// The render graph identification is read from
    /// <see cref="IHasIdentification.Identification"/> on <typeparamref name="TRenderGraph"/>.
    /// </summary>
    TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification;
}
