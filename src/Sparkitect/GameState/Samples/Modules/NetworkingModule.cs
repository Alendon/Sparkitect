using JetBrains.Annotations;
using Sparkitect.GameState;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.RegisterModule("networking")]
public sealed partial class NetworkingModule : IStateModule
{
    public const string Key_NetworkTick = "network_tick";

    [StateFunction(Key_NetworkTick)]
    [PerFrame]
    public static void NetworkTick(FeatureContext ctx)
    {
        // Networking pump placeholder
        _ = ctx;
    }

    public static IReadOnlyList<Type> UsedServices => [];
}