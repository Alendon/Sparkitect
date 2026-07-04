using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.GameState;

/// <summary>
/// Capture-before-delete safety net: pins the resolved composed set for each of the three
/// sample states (Minimal / Pong / SpaceInvaders) to the goldens captured from the CURRENT hand-written
/// topologies, BEFORE the migration sweep replaces those lists. The assertion is SET-equality
/// (order-insensitive) — never the order-sensitive idiom used by 57.1.
///
/// Fixture modelling notes:
/// - The RenderGraph -> Vulkan direct edge is modelled explicitly (RenderGraph.Requires = [Vulkan]) and
///   is load-bearing: no state lists Vulkan directly, so Vulkan must arrive purely through the closure.
/// - Under the new model Core is NOT in any module's Requires — it arrives purely from the root ambient
///   seed { Core, Settings, UserSettings }, proving ambient behaviour at the composer level.
/// - Leaf game modules require the same engine modules the real modules require today, minus Core.
/// - Ids are synthetic; only set membership carries a contract.
/// </summary>
public class CompositionRegressionTests
{
    // Root ambient seed members.
    private static readonly Identification Core = Identification.Create(1, 1, 1);
    private static readonly Identification Settings = Identification.Create(1, 1, 2);
    private static readonly Identification UserSettings = Identification.Create(1, 1, 3);

    // Engine modules.
    private static readonly Identification Vulkan = Identification.Create(1, 1, 10);
    private static readonly Identification Windowing = Identification.Create(1, 1, 11);
    private static readonly Identification RenderGraph = Identification.Create(1, 1, 12);
    private static readonly Identification Ecs = Identification.Create(1, 1, 13);

    // Synthetic sample game-module leaves.
    private static readonly Identification SampleLeaf = Identification.Create(10, 1, 1);
    private static readonly Identification PongLeaf = Identification.Create(11, 1, 1);
    private static readonly Identification SpaceInvadersLeaf = Identification.Create(12, 1, 1);

    private static readonly Identification StateId = Identification.Create(2, 1, 1);

    private static ModuleComposition Mod(Identification id, params Identification[] requires)
        => new(id, requires, []);

    private static HashSet<Identification> RootSeed() => [Core, Settings, UserSettings];

    /// <summary>
    /// The registered-module universe shared by all three sample topologies. Core/Settings/UserSettings
    /// are seed-only (never registered, never required) — they arrive ambiently. RenderGraph carries the
    /// single real transitive edge to Vulkan.
    /// </summary>
    private static Dictionary<Identification, ModuleComposition> SampleRegistry()
        => new[]
        {
            Mod(Vulkan),
            Mod(Windowing),
            Mod(Ecs),
            Mod(RenderGraph, Vulkan),                 // real transitive edge
            Mod(SampleLeaf),                           // real SampleModule required only [Core] -> [] after Core drop
            Mod(PongLeaf, Vulkan, Windowing),          // real PongModule required [Core, Vulkan, Windowing]
            Mod(SpaceInvadersLeaf, Vulkan, Windowing), // real SpaceInvadersModule required [Core, Vulkan, Windowing]
        }.ToDictionary(m => m.Id);

    [Test]
    public async Task Minimal_ComposedSet_MatchesGolden()
    {
        // Golden: { Core, Settings, UserSettings, Sample, Vulkan, Ecs, RenderGraph, Windowing }.
        var registry = SampleRegistry();
        var state = new StateComposition(StateId, Identification.Empty, [SampleLeaf, Ecs, RenderGraph, Windowing]);
        var golden = new HashSet<Identification>
        {
            Core, Settings, UserSettings, SampleLeaf, Vulkan, Ecs, RenderGraph, Windowing,
        };

        var result = StateComposer.Compose(state, RootSeed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(golden)).IsTrue();
        // Core is ambient — no module declared it in Requires.
        await Assert.That(registry.Values.Any(m => m.Requires.Contains(Core))).IsFalse();
        await Assert.That(result.ComposedSet.Contains(Core)).IsTrue();
    }

    [Test]
    public async Task Pong_ComposedSet_MatchesGolden()
    {
        // Golden: { Core, Settings, UserSettings, Pong, Vulkan, RenderGraph, Windowing }.
        var registry = SampleRegistry();
        var state = new StateComposition(StateId, Identification.Empty, [PongLeaf, RenderGraph, Windowing]);
        var golden = new HashSet<Identification>
        {
            Core, Settings, UserSettings, PongLeaf, Vulkan, RenderGraph, Windowing,
        };

        var result = StateComposer.Compose(state, RootSeed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(golden)).IsTrue();
        // Vulkan is load-bearing via the RenderGraph closure (state does not list it directly).
        await Assert.That(state.DirectModules.Contains(Vulkan)).IsFalse();
        await Assert.That(result.ComposedSet.Contains(Vulkan)).IsTrue();
    }

    [Test]
    public async Task SpaceInvaders_ComposedSet_MatchesGolden()
    {
        // Golden: { Core, Settings, UserSettings, SpaceInvaders, Vulkan, RenderGraph, Windowing, Ecs }.
        var registry = SampleRegistry();
        var state = new StateComposition(
            StateId, Identification.Empty, [SpaceInvadersLeaf, Ecs, RenderGraph, Windowing]);
        var golden = new HashSet<Identification>
        {
            Core, Settings, UserSettings, SpaceInvadersLeaf, Vulkan, RenderGraph, Windowing, Ecs,
        };

        var result = StateComposer.Compose(state, RootSeed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(golden)).IsTrue();
        await Assert.That(result.ComposedSet.Contains(Core)).IsTrue();
    }
}
