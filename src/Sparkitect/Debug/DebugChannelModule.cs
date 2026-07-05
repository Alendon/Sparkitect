using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Debug;

/// <summary>
/// Composition module for the engine debug channel. It declares no direct requirements and instead
/// auto-activates alongside the core module via <see cref="ActivatesWith"/>, so it composes into every
/// state whose resolved set contains Core without any state referencing it. Its composition inclusion is
/// gated by the <c>debug_channel</c> setting at the game-state finalize seam; with that setting off the
/// module is dropped before compose and is absent from every composed set (not merely inert).
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("debug_channel")]
public sealed partial class DebugChannelModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc />
    public override IReadOnlyList<Identification> Requires => [];

    /// <inheritdoc />
    public override IReadOnlyList<Identification> ActivatesWith => [StateModuleID.Sparkitect.Core];

    // The channel host is a DI-resolved subsystem reached only through the injected parameter (no module
    // state). It brings itself online on the first republish — i.e. on the first frame enter, once the
    // bootstrapper has completed — and only because this module composed, which happens only when the setting
    // is on. Every frame enter/exit is a composition change, so both republish; when the debuggee is paused
    // no transition runs and the client's cached snapshot simply holds (no refresh-on-pause machinery).
    [OnFrameEnterScheduling]
    [TransitionFunction("publish_debug_snapshot_on_enter")]
    static void PublishOnFrameEnter(IDebugChannelServer channelServer) => channelServer.Republish();

    [OnFrameExitScheduling]
    [TransitionFunction("publish_debug_snapshot_on_exit")]
    static void PublishOnFrameExit(IDebugChannelServer channelServer) => channelServer.Republish();
}
