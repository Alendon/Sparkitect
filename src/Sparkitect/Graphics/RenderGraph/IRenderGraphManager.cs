using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Read surface for the render-graph subsystem: pass catalog, render-graph-type catalog, resource-moment
/// catalog, plus the construction entry point <see cref="CreateGraph{TRenderGraph}"/>. Construction is
/// thin and GameState-owned — there is no resource-manager catalog, no child container, and no per-graph
/// service-list metadata.
/// </summary>
[RegistryFacade<IRenderGraphManagerRegistryFacade>]
[PublicAPI]
public interface IRenderGraphManager
{
    /// <summary>All pass identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredPassIds { get; }

    /// <summary>All render-graph-type identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredRenderGraphIds { get; }

    /// <summary>
    /// All registered resource moments keyed by identification, each carrying the resource type its
    /// moment conveys across passes. Backed by the demoted moment collection on the manager.
    /// </summary>
    IReadOnlyDictionary<Identification, ResourceMomentDefinition> RegisteredResourceMoments { get; }

    /// <summary>
    /// Builds and returns a ready-to-run render graph of type <typeparamref name="TRenderGraph"/>
    /// wired with the supplied <paramref name="passIds"/> and target <paramref name="window"/>. The
    /// render-graph identification is read from <see cref="IHasIdentification.Identification"/> on
    /// <typeparamref name="TRenderGraph"/>.
    /// </summary>
    TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification;
}
