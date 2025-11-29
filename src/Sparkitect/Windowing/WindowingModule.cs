using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Windowing;

[ModuleRegistry.RegisterModule("windowing")]
public partial class WindowingModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    public static Identification Identification => StateModuleID.Sparkitect.Windowing;

    [StateFunction("create_window")]
    [OnCreate]
    public static void CreateWindow(IWindowManager windowManager)
    {
        windowManager.CreateWindow("Sparkitect", 800, 600);
    }

    [StateFunction("poll_window")]
    [PerFrame]
    public static void PollWindow(IWindowManager windowManager, IGameStateManager stateManager)
    {
        windowManager.PollEvents();

        if (!windowManager.IsOpen)
        {
            Log.Information("Window closed, requesting shutdown");
            stateManager.Shutdown();
        }
    }

    [StateFunction("close_window")]
    [OnDestroy]
    public static void CloseWindow(IWindowManager windowManager)
    {
        windowManager.Close();
    }
}