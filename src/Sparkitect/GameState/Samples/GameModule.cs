using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples;

[PublicAPI]
[StateModule("game")]
[OrderAfterModule(typeof(RenderingModule))]
public sealed partial class GameModule
{
    public const string Key_SimulationTick = "simulation_tick";

    [Feature(Key_SimulationTick)]
    public static void SimulationTick(FeatureContext ctx)
    {
        // Simulation tick placeholder
        _ = ctx;
    }
}

