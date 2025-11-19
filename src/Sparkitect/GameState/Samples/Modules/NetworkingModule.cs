using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

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

    public static Span<Identification> RequiredModules => [];
    public static Identification Identification => StateModuleID.Sparkitect.Networking;
}