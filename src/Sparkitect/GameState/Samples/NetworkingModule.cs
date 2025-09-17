using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples;

[PublicAPI]
[StateModule("networking")]
[OrderAfterModule(typeof(GameModule))]
public sealed partial class NetworkingModule
{
    public const string Key_NetworkTick = "network_tick";

    [Feature(Key_NetworkTick)]
    public static void NetworkTick(FeatureContext ctx)
    {
        // Networking pump placeholder
        _ = ctx;
    }
}

