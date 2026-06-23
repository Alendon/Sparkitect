using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Shared render-graph infrastructure module. Registers the pass, resource, and
/// render-graph-type registries and finalizes the consolidated manager via
/// <see cref="IRenderGraphManagerStateFacade.PostRegistry"/> after registration runs.
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
        registryManager.AddRegistry<RenderPassRegistry>();
        registryManager.AddRegistry<GraphResourceRegistry>();
        registryManager.AddRegistry<RenderGraphRegistry>();
        registryManager.AddRegistry<GraphImageRegistry>();
    }

    [TransitionFunction("process_render_graph_registries")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddRenderGraphRegistriesFunc>]
    public static void ProcessRegistries(
        IRegistryManager registryManager,
        IRenderGraphManagerStateFacade manager)
    {
        registryManager.ProcessAllMissing<RenderPassRegistry>();
        registryManager.ProcessAllMissing<GraphResourceRegistry>();
        registryManager.ProcessAllMissing<RenderGraphRegistry>();
        registryManager.ProcessAllMissing<GraphImageRegistry>();
        manager.PostRegistry();
    }

    [TransitionFunction("remove_render_graph_registries")]
    [OnDestroyScheduling]
    public static void RemoveRegistries(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<RenderPassRegistry>();
        registryManager.UnregisterAllRemaining<GraphResourceRegistry>();
        registryManager.UnregisterAllRemaining<RenderGraphRegistry>();
        registryManager.UnregisterAllRemaining<GraphImageRegistry>();
    }
}
