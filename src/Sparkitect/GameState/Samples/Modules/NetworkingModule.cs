using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.Register("networking")]
[OrderAfterModule(typeof(GameModule))]
public sealed partial class NetworkingModule : IStateModule
{
    public const string Key_NetworkTick = "network_tick";

    [Feature(Key_NetworkTick)]
    public static void NetworkTick(FeatureContext ctx)
    {
        // Networking pump placeholder
        _ = ctx;
    }
}

