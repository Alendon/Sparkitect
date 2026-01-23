using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

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

    [TransitionFunction("pong_init")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessRegistriesFunc>]
    public static void Initialize(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Initialize();
        Log.Information("Pong state initialized");
    }

    [PerFrameFunction("pong_frame")]
    [PerFrameScheduling]
    public static void Frame(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Tick();
        pongRuntime.Render();
    }

    [TransitionFunction("pong_cleanup")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.DestroyDeviceFunc>]
    public static void Cleanup(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Cleanup();
        Log.Information("Pong state cleanup");
    }

    interface A<B>
    {
        public void Do(B value);
    }

    class C : A<string>, A<int>
    {
        public void Do(string value)
        {
            throw new NotImplementedException();
        }

        public void Do(int value)
        {
            throw new NotImplementedException();
        }
    }
}
