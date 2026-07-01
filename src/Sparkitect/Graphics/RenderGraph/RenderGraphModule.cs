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
public partial class RenderGraphModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Vulkan];

    [TransitionFunction("process_render_graph_registries_enter")]
    [OnFrameEnterScheduling]
    public static void ProcessRegistriesEnter(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ResourceMomentRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderPassRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<FactRegistry, RenderGraphModule>();
        registryManager.ProcessRegistry<RenderGraphRegistry, RenderGraphModule>();
    }

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
