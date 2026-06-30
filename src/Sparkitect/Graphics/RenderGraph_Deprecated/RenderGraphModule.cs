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
[ModuleRegistry.RegisterModule("render_graph_deprecated")]
[PublicAPI]
public partial class RenderGraphDeprecatedModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Vulkan];

    [TransitionFunction("add_render_graph_registries_deprecated")]
    [OnCreateScheduling]
    public static void AddRegistries(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<RenderPassDeprecatedRegistry>();
        registryManager.AddRegistry<GraphResourceRegistry>();
        registryManager.AddRegistry<RenderGraphDeprecatedRegistry>();
        registryManager.AddRegistry<GraphImageRegistry>();
    }

    [TransitionFunction("process_render_graph_registries_deprecated")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddRenderGraphRegistriesDeprecatedFunc>]
    public static void ProcessRegistries(
        IRegistryManager registryManager,
        IRenderGraphManagerStateFacade manager)
    {
        registryManager.ProcessAllMissing<RenderPassDeprecatedRegistry>();
        registryManager.ProcessAllMissing<GraphResourceRegistry>();
        registryManager.ProcessAllMissing<RenderGraphDeprecatedRegistry>();
        registryManager.ProcessAllMissing<GraphImageRegistry>();
        manager.PostRegistry();
    }

    [TransitionFunction("remove_render_graph_registries_deprecated")]
    [OnDestroyScheduling]
    public static void RemoveRegistries(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<RenderPassDeprecatedRegistry>();
        registryManager.UnregisterAllRemaining<GraphResourceRegistry>();
        registryManager.UnregisterAllRemaining<RenderGraphDeprecatedRegistry>();
        registryManager.UnregisterAllRemaining<GraphImageRegistry>();
    }
}
