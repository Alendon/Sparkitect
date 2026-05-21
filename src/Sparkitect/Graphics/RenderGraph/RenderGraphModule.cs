using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Shared render-graph infrastructure module. Registers the <see cref="RenderPassRegistry"/>
/// into the per-state registry pipeline. Does not own <see cref="RenderGraph"/> instances —
/// consumers create those via <see cref="RenderGraph.Initialize"/> and own them themselves.
/// </summary>
[ModuleRegistry.RegisterModule("render_graph")]
[PublicAPI]
public partial class RenderGraphModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Vulkan];

    [TransitionFunction("add_render_pass_registry")]
    [OnCreateScheduling]
    public static void AddRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<RenderPassRegistry>();
    }

    [TransitionFunction("add_graph_resource_registry")]
    [OnCreateScheduling]
    public static void AddGraphResourceRegistry(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<GraphResourceRegistry>();
    }

    [TransitionFunction("process_render_pass_registry")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddRenderPassRegistryFunc>]
    public static void ProcessRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.ProcessAllMissing<RenderPassRegistry>();
    }

    [TransitionFunction("process_graph_resource_registry")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddGraphResourceRegistryFunc>]
    public static void ProcessGraphResourceRegistry(
        IRegistryManager registryManager,
        IGraphResourceTypesStateFacade resourceTypes)
    {
        registryManager.ProcessAllMissing<GraphResourceRegistry>();
        resourceTypes.PostProcess();
    }

    [TransitionFunction("remove_render_pass_registry")]
    [OnDestroyScheduling]
    public static void RemoveRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<RenderPassRegistry>();
    }

    [TransitionFunction("remove_graph_resource_registry")]
    [OnDestroyScheduling]
    public static void RemoveGraphResourceRegistry(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<GraphResourceRegistry>();
    }
}
