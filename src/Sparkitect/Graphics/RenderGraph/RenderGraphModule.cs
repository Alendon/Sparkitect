using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Moments;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Render-graph infrastructure module. Registers the pass, render-graph-type, and resource-moment
/// registries. The resource-moment registry must be processed before any graph Setup so reserved moment
/// ids (e.g. the finishline) are assigned before pass declarations reference them.
/// </summary>
[ModuleRegistry.RegisterModule("render_graph")]
[PublicAPI]
public partial class RenderGraphModule : TransitiveStateModule, IHasIdentification
{
    /// <summary>Modules this module directly depends on; the render graph requires the Vulkan module.</summary>
    public override IReadOnlyList<Identification> Requires => [StateModuleID.Sparkitect.Vulkan];

    /// <summary>Processes the render-graph registries on state entry, moments first so reserved moment ids exist before pass declarations reference them.</summary>
    [TransitionFunction("process_render_graph_registries_enter")]
    [OnFrameEnterScheduling]
    public static void ProcessRegistriesEnter(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ResourceMomentRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderPassRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<FactRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderGraphRegistry, RenderGraphModule>();
    }

    /// <summary>Re-processes the render-graph registries on state exit, unregistering the departing state's contributions.</summary>
    [TransitionFunction("process_render_graph_registries_exit")]
    [OnFrameExitScheduling]
    public static void ProcessRegistriesExit(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ResourceMomentRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderPassRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<FactRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderGraphRegistry, RenderGraphModule>();
    }
}
