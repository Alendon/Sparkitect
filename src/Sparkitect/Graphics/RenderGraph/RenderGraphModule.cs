using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Moments;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Clean-room render-graph infrastructure module. Registers the pass, render-graph-type, and
/// resource-moment registries; construction is thin and GameState-owned, so there is no post-registry
/// finalization. The resource-moment registry must be processed before any graph Setup so the reserved
/// moment ids (e.g. the finishline) are assigned before pass declarations reference them.
/// </summary>
[ModuleRegistry.RegisterModule("render_graph")]
[PublicAPI]
public partial class RenderGraphModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Vulkan];

    [TransitionFunction("add_render_graph_registries")]
    [OnCreateScheduling]
    public static void AddRegistries(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<ResourceMomentRegistry>();
        registryManager.AddRegistry<RenderPassRegistry>();
        registryManager.AddRegistry<FactRegistry>();
        registryManager.AddRegistry<RenderGraphRegistry>();
    }

    [TransitionFunction("process_render_graph_registries")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddRenderGraphRegistriesFunc>]
    public static void ProcessRegistries(IRegistryManager registryManager)
    {
        registryManager.ProcessAllMissing<ResourceMomentRegistry>();
        registryManager.ProcessAllMissing<RenderPassRegistry>();
        registryManager.ProcessAllMissing<FactRegistry>();
        registryManager.ProcessAllMissing<RenderGraphRegistry>();
    }

    [TransitionFunction("remove_render_graph_registries")]
    [OnDestroyScheduling]
    public static void RemoveRegistries(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<ResourceMomentRegistry>();
        registryManager.UnregisterAllRemaining<RenderPassRegistry>();
        registryManager.UnregisterAllRemaining<FactRegistry>();
        registryManager.UnregisterAllRemaining<RenderGraphRegistry>();
    }
}
