using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Shared render-graph infrastructure module. Registers the <see cref="RenderPassRegistry"/>
/// into the per-state registry pipeline (RG-31 initial form per D-C9).
/// </summary>
/// <remarks>
/// <para>
/// Per D-C9 / D-E4 this module ships shared infrastructure ONLY — pass registry add/process/remove
/// transitions. It does not own <see cref="RenderGraph"/> instances; consumers create those
/// themselves via the <see cref="RenderGraph.Initialize"/> static factory and own them as
/// per-state fields (mirrors the <c>IWorld.Create()</c> + per-state ownership pattern).
/// </para>
/// <para>
/// <see cref="RequiredModules"/> declares <c>StateModuleID.Sparkitect.Vulkan</c> so a graph
/// state cannot activate this module without <see cref="Sparkitect.Graphics.Vulkan.VulkanModule"/>
/// being active first — the <see cref="RenderGraph.Initialize"/> factory needs
/// <see cref="Sparkitect.Graphics.Vulkan.IVulkanContext"/> available at resolve time.
/// </para>
/// </remarks>
[ModuleRegistry.RegisterModule("render_graph")]
public partial class RenderGraphModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Vulkan];

    [TransitionFunction("add_render_pass_registry")]
    [OnCreateScheduling]
    public static void AddRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<RenderPassRegistry>();
    }

    [TransitionFunction("process_render_pass_registry")]
    [OnFrameEnterScheduling]
    [OrderAfter<AddRenderPassRegistryFunc>]
    public static void ProcessRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.ProcessAllMissing<RenderPassRegistry>();
    }

    [TransitionFunction("remove_render_pass_registry")]
    [OnDestroyScheduling]
    public static void RemoveRenderPassRegistry(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<RenderPassRegistry>();
    }
}
