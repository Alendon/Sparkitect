using Sparkitect.Debug;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Debug;

/// <summary>
/// Pins the D-20 composition-inclusion gate outcome. The debug module is modelled as an integration module
/// (<c>ActivatesWith =&gt; [Core]</c>) keyed under the production <see cref="DebugModuleGate.ModuleId"/>; the
/// test drives the real gate's on/off decision and asserts composed-set membership of a Core-bearing state.
/// It tests the OUTCOME — module present/absent in a composed set — never the registration itself. The Core
/// id is synthetic (module ids are runtime-assigned; the pure composer is id-agnostic under set semantics).
/// </summary>
public class DebugModuleGateTests
{
    private static readonly Identification Core = Identification.Create(2, 1, 50);
    private static readonly Identification StateId = Identification.Create(2, 1, 51);

    // A Core-bearing state plus the debug module auto-activating on Core, keyed under the production id.
    private static Dictionary<Identification, ModuleComposition> BuildUniverse() => new()
    {
        [Core] = new(Core, [], []),
        [DebugModuleGate.ModuleId] = new(DebugModuleGate.ModuleId, [], [Core]),
    };

    private static StateComposition CoreBearingState() => new(StateId, Identification.Empty, [Core]);

    [Test]
    public async Task ChannelEnabled_DebugModuleComposesIntoCoreBearingState()
    {
        var universe = BuildUniverse();
        DebugModuleGate.ExcludeWhenDisabled(universe, channelEnabled: true);

        var result = StateComposer.Compose(CoreBearingState(), new HashSet<Identification>(), universe);

        await Assert.That(result.ComposedSet.Contains(DebugModuleGate.ModuleId)).IsTrue();
    }

    [Test]
    public async Task ChannelDisabled_DebugModuleAbsentFromComposedSet()
    {
        var universe = BuildUniverse();
        DebugModuleGate.ExcludeWhenDisabled(universe, channelEnabled: false);

        var result = StateComposer.Compose(CoreBearingState(), new HashSet<Identification>(), universe);

        await Assert.That(result.ComposedSet.Contains(DebugModuleGate.ModuleId)).IsFalse();
        // The gate is debug-specific: Core (and any other module) is unaffected.
        await Assert.That(result.ComposedSet.Contains(Core)).IsTrue();
    }
}
