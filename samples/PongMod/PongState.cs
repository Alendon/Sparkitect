using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace PongMod;

[StateRegistry.RegisterState("pong")]
public partial class PongState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.PongMod.Pong;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.PongMod.Pong,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Windowing
    ];

    [StateFunction("pong_init")]
    [OrderAfter<VulkanModule>(VulkanModule.ProcessRegistries_Key)]
    [OnCreate]
    public static void Initialize(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Initialize();
        Log.Information("Pong state initialized");
    }

    [StateFunction("pong_frame")]
    [PerFrame]
    public static void Frame(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Tick();
        pongRuntime.Render();
    }
    
    [StateFunction("pong_cleanup")]
    [OnDestroy]
    [OrderBefore<VulkanModule>(VulkanModule.DestroyDevice_Key)]
    public static void Cleanup(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Cleanup();
        Log.Information("Pong state cleanup");
    }
}
