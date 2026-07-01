using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Read surface for the render-graph subsystem (pass, fact, render-graph-type, and resource-moment
/// catalogs) plus the construction entry point <see cref="CreateGraph{TRenderGraph}"/>. Each graph is
/// resolved against a per-graph child container populated with its <c>[GraphLocal&lt;,IRenderGraph&gt;]</c> services.
/// </summary>
[RegistryFacade<IRenderGraphManagerRegistryFacade>]
[PublicAPI]
public interface IRenderGraphManager
{
    /// <summary>All pass identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredPassIds { get; }

    /// <summary>All fact identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredFactIds { get; }

    /// <summary>All render-graph-type identifications registered with the render-graph subsystem.</summary>
    IReadOnlyCollection<Identification> RegisteredRenderGraphIds { get; }

    /// <summary>All registered resource moments keyed by identification, each carrying the resource type it conveys across passes.</summary>
    IReadOnlyDictionary<Identification, ResourceMomentDefinition> RegisteredResourceMoments { get; }

    /// <summary>
    /// Builds a ready-to-run render graph of type <typeparamref name="TRenderGraph"/> wired with
    /// <paramref name="passIds"/> and target <paramref name="window"/>.
    /// </summary>
    TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification;
}
