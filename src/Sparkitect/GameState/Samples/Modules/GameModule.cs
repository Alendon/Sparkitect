using JetBrains.Annotations;
using Sparkitect.GameState;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.Register("game")]
[OrderAfterModule(typeof(RenderingModule))]
public sealed partial class GameModule : IStateModule
{
    public const string Key_SimulationTick = "simulation_tick";

    [Feature(Key_SimulationTick)]
    public static void SimulationTick(FeatureContext ctx)
    {
        // Simulation tick placeholder
        _ = ctx;
    }
}

