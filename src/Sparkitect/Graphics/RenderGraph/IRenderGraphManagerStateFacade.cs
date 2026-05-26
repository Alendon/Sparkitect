using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// State-function facade for <see cref="IRenderGraphManager"/>. Used by the
/// render-graph module's process-registries transition to finalize the catalog by running
/// the metadata pipelines that bind tracked ids to managers, services, and graphs.
/// </summary>
[FacadeFor<IRenderGraphManager>]
[PublicAPI]
public interface IRenderGraphManagerStateFacade
{
    /// <summary>
    /// Runs after all three render-graph registries have been processed. Resolves
    /// ResourceManagerBinding metadata, RGServiceListMetadata Layer-1 entries, and
    /// IGraphLocalServiceEntry Layer-2 contributors into the manager's internal
    /// dictionaries. Idempotent — safe to call more than once per state cycle.
    /// </summary>
    void PostRegistry();
}
