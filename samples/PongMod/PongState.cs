using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
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
        // TODO: Input handling (stub for now)
        // pongRuntime.MoveLeftPaddle(inputDelta);
        // pongRuntime.MoveRightPaddle(inputDelta);

        // Tick simulation (handles timing internally)
        pongRuntime.Tick();

        // TODO: Render (stub - will call compute shader)
    }

    [StateFunction("pong_cleanup")]
    [OnDestroy]
    public static void Cleanup()
    {
        Log.Information("Pong state cleanup");
    }
}
