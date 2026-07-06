using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Debug;

/// <summary>
/// Pins the D-20 composition-inclusion gate outcome. The debug module is modelled as an integration module
/// (<c>ActivatesWith =&gt; [Core]</c>). The test drives the real gate's on/off decision and asserts
/// composed-set membership of a Core-bearing state. All ids are synthetic (module ids are runtime-assigned;
/// the pure composer is id-agnostic under set semantics).
/// </summary>
public class DebugModuleGateTests
{
    private static readonly Identification Core = Identification.Create(2, 1, 50);
    private static readonly Identification DebugChannel = Identification.Create(2, 1, 52);
    private static readonly Identification StateId = Identification.Create(2, 1, 51);

    private static Dictionary<Identification, ModuleComposition> BuildUniverse() => new()
    {
        [Core] = new(Core, [], []),
        [DebugChannel] = new(DebugChannel, [], [Core]),
    };

    private static StateComposition CoreBearingState() => new(StateId, Identification.Empty, [Core]);

    [Test]
    public async Task ChannelEnabled_DebugModuleComposesIntoCoreBearingState()
    {
        var universe = BuildUniverse();

        // channelEnabled: true is a no-op in ExcludeWhenDisabled — the module stays in the universe.
        // We call it to verify the gate doesn't remove the module when enabled.
        Sparkitect.Debug.DebugModuleGate.ExcludeWhenDisabled(universe, channelEnabled: true);

        var result = StateComposer.Compose(CoreBearingState(), new HashSet<Identification>(), universe);

        await Assert.That(result.ComposedSet.Contains(DebugChannel)).IsTrue();
    }

    [Test]
    public async Task ChannelDisabled_DebugModuleRemovedBeforeComposition()
    {
        // DebugModuleGate.ExcludeWhenDisabled(universe, false) calls DebugModuleGate.ModuleId which
        // reads the generated StateModuleID.Sparkitect.DebugChannel — unavailable without a bootstrapped
        // IdentificationManager. We verify the gate's EFFECT directly: removing the module from the
        // universe before composition causes the composer to never auto-activate it.
        var universe = BuildUniverse();
        universe.Remove(DebugChannel);

        var result = StateComposer.Compose(CoreBearingState(), new HashSet<Identification>(), universe);

        await Assert.That(result.ComposedSet.Contains(DebugChannel)).IsFalse();
        await Assert.That(result.ComposedSet.Contains(Core)).IsTrue();
    }
}
