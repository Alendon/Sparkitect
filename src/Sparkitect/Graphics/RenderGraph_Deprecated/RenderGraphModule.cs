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

    [TransitionFunction("process_render_graph_registries_deprecated_enter")]
    [OnFrameEnterScheduling]
    public static void ProcessRegistriesEnter(
        IRegistryManager registryManager,
        IRenderGraphManagerStateFacade manager)
    {
        registryManager.ProcessRegistry<RenderPassDeprecatedRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<GraphResourceRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<RenderGraphDeprecatedRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<GraphImageRegistry, RenderGraphDeprecatedModule>();
        manager.PostRegistry();
    }

    [TransitionFunction("process_render_graph_registries_deprecated_exit")]
    [OnFrameExitScheduling]
    public static void ProcessRegistriesExit(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<RenderPassDeprecatedRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<GraphResourceRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<RenderGraphDeprecatedRegistry, RenderGraphDeprecatedModule>();
        registryManager.ProcessRegistry<GraphImageRegistry, RenderGraphDeprecatedModule>();
    }
}
