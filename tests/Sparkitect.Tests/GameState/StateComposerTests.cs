using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.GameState;

/// <summary>
/// Pins the pure closure-resolution behaviour of <see cref="StateComposer"/>: transitive expansion
/// over module Requires, cycle termination, ambient parent-chain seeding (Core arrives without any
/// module declaring it), and delta computation. All ids are synthetic — the composer is pure over
/// <see cref="Identification"/> and never touches the generated id framework.
/// </summary>
public class StateComposerTests
{
    // Synthetic engine-module ids (values arbitrary; only membership matters under set semantics).
    private static readonly Identification Core = Identification.Create(1, 1, 1);
    private static readonly Identification Settings = Identification.Create(1, 1, 2);
    private static readonly Identification UserSettings = Identification.Create(1, 1, 3);
    private static readonly Identification Vulkan = Identification.Create(1, 1, 10);
    private static readonly Identification Windowing = Identification.Create(1, 1, 11);
    private static readonly Identification RenderGraph = Identification.Create(1, 1, 12);

    private static readonly Identification StateId = Identification.Create(2, 1, 1);

    private static ModuleComposition Mod(Identification id, params Identification[] requires)
        => new(id, requires, []);

    private static Dictionary<Identification, ModuleComposition> Registry(params ModuleComposition[] mods)
        => mods.ToDictionary(m => m.Id);

    private static HashSet<Identification> Seed(params Identification[] ids) => [.. ids];

    [Test]
    public async Task Compose_TransitiveRequires_ExpandsClosure()
    {
        // RenderGraph -> Vulkan is the real transitive edge; a state that only lists RenderGraph
        // must still resolve Vulkan into its composed set.
        var registry = Registry(
            Mod(RenderGraph, Vulkan),
            Mod(Vulkan));
        var state = new StateComposition(StateId, Identification.Empty, [RenderGraph]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(RenderGraph)).IsTrue();
        await Assert.That(result.ComposedSet.Contains(Vulkan)).IsTrue();
    }

    [Test]
    public async Task Compose_MultiHopClosure_PullsAllTransitiveModules()
    {
        // A -> B -> C: declaring only A must resolve B and C.
        var a = Identification.Create(3, 1, 1);
        var b = Identification.Create(3, 1, 2);
        var c = Identification.Create(3, 1, 3);
        var registry = Registry(Mod(a, b), Mod(b, c), Mod(c));
        var state = new StateComposition(StateId, Identification.Empty, [a]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(Seed(a, b, c))).IsTrue();
    }

    [Test]
    public async Task Compose_CycleInRequires_Terminates()
    {
        // Cycles are harmless — the visited-set walk terminates and reports no error.
        var a = Identification.Create(3, 2, 1);
        var b = Identification.Create(3, 2, 2);
        var registry = Registry(Mod(a, b), Mod(b, a));
        var state = new StateComposition(StateId, Identification.Empty, [a]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(Seed(a, b))).IsTrue();
    }

    [Test]
    public async Task Compose_AmbientSeed_CorePresentThoughNoModuleRequiresIt()
    {
        // Core arrives purely from the ambient parent-chain seed. No module in the
        // registry declares Core in Requires, yet it appears in the child state's composed set.
        var registry = Registry(Mod(Vulkan), Mod(Windowing));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan, Windowing]);
        var seed = Seed(Core, Settings, UserSettings);

        var result = StateComposer.Compose(state, seed, registry);

        await Assert.That(result.ComposedSet.Contains(Core)).IsTrue();
        // Prove Core came from the seed, not a Requires declaration.
        await Assert.That(registry.Values.Any(m => m.Requires.Contains(Core))).IsFalse();
    }

    [Test]
    public async Task Compose_Delta_IsComposedSetMinusParentChainSeed()
    {
        // Delta = ComposedSet - parent-chain seed. Seed members (Core) are excluded from the delta;
        // newly-added modules (Vulkan, Windowing) are included.
        var registry = Registry(Mod(Vulkan), Mod(Windowing));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan, Windowing]);
        var seed = Seed(Core);

        var result = StateComposer.Compose(state, seed, registry);

        var delta = result.Delta.ToHashSet();
        await Assert.That(delta.SetEquals(Seed(Vulkan, Windowing))).IsTrue();
        await Assert.That(delta.Contains(Core)).IsFalse();
    }

    [Test]
    public async Task Compose_SeedMemberAlsoDeclaredDirect_IsNoEvent()
    {
        // Direct + ambient arrival of the same module is a non-event (set semantics, no warning,
        // no throw, not in delta).
        var registry = Registry(Mod(Vulkan));
        var state = new StateComposition(StateId, Identification.Empty, [Core, Vulkan]);
        var seed = Seed(Core);

        var result = StateComposer.Compose(state, seed, registry);

        await Assert.That(result.ComposedSet.SetEquals(Seed(Core, Vulkan))).IsTrue();
        await Assert.That(result.Delta.ToHashSet().SetEquals(Seed(Vulkan))).IsTrue();
    }
}
