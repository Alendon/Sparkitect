using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.GameState;

/// <summary>
/// Pins ActivatesWith fixpoint auto-activation: a module whose ActivatesWith targets
/// are all present auto-composes (together with its own Requires closure); a module with any absent
/// target stays out; cascades converge; auto-activated modules are indistinguishable from hard-required
/// ones in the resulting set. All ids synthetic — the composer is pure over <see cref="Identification"/>.
/// </summary>
public class ActivatesWithTests
{
    private static readonly Identification Vulkan = Identification.Create(1, 1, 10);
    private static readonly Identification RenderGraph = Identification.Create(1, 1, 12);

    private static readonly Identification StateId = Identification.Create(2, 1, 1);

    private static ModuleComposition Mod(Identification id, params Identification[] requires)
        => new(id, requires, []);

    private static ModuleComposition Integration(
        Identification id, Identification[] activatesWith, params Identification[] requires)
        => new(id, requires, activatesWith);

    private static Dictionary<Identification, ModuleComposition> Registry(params ModuleComposition[] mods)
        => mods.ToDictionary(m => m.Id);

    private static HashSet<Identification> Seed(params Identification[] ids) => [.. ids];

    [Test]
    public async Task Activation_AllTargetsPresent_ModuleAutoComposes()
    {
        var integration = Identification.Create(5, 1, 1);
        var registry = Registry(
            Mod(Vulkan),
            Mod(RenderGraph),
            Integration(integration, [Vulkan, RenderGraph]));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan, RenderGraph]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(integration)).IsTrue();
    }

    [Test]
    public async Task Activation_OneTargetAbsent_ModuleStaysOut()
    {
        var integration = Identification.Create(5, 1, 2);
        var registry = Registry(
            Mod(Vulkan),
            Mod(RenderGraph),
            Integration(integration, [Vulkan, RenderGraph]));
        // Only Vulkan declared — RenderGraph absent, so the integration must NOT activate.
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(integration)).IsFalse();
        await Assert.That(result.ComposedSet.Contains(RenderGraph)).IsFalse();
    }

    [Test]
    public async Task Activation_ActivatedModuleRequires_PulledInTransitively()
    {
        // Integration activates on Vulkan and itself requires an extra module — that require must
        // be pulled into the set transitively.
        var integration = Identification.Create(5, 2, 1);
        var extra = Identification.Create(5, 2, 2);
        var registry = Registry(
            Mod(Vulkan),
            Mod(extra),
            Integration(integration, [Vulkan], extra));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(integration)).IsTrue();
        await Assert.That(result.ComposedSet.Contains(extra)).IsTrue();
    }

    [Test]
    public async Task Activation_Cascade_ModuleBActivatesAfterModuleA()
    {
        // A activates on Vulkan; B activates on A. B must only enter after A (itself auto-activated)
        // has entered — proving the fixpoint re-scans until convergence.
        var moduleA = Identification.Create(5, 3, 1);
        var moduleB = Identification.Create(5, 3, 2);
        var registry = Registry(
            Mod(Vulkan),
            Integration(moduleA, [Vulkan]),
            Integration(moduleB, [moduleA]));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(moduleA)).IsTrue();
        await Assert.That(result.ComposedSet.Contains(moduleB)).IsTrue();
    }

    [Test]
    public async Task Activation_CascadeBroken_DownstreamModuleStaysOut()
    {
        // B activates on A; A activates on Vulkan which is absent. Neither A nor B may enter.
        var moduleA = Identification.Create(5, 4, 1);
        var moduleB = Identification.Create(5, 4, 2);
        var registry = Registry(
            Mod(Vulkan),
            Integration(moduleA, [Vulkan]),
            Integration(moduleB, [moduleA]));
        var state = new StateComposition(StateId, Identification.Empty, []);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.Contains(moduleA)).IsFalse();
        await Assert.That(result.ComposedSet.Contains(moduleB)).IsFalse();
    }

    [Test]
    public async Task Activation_AutoActivatedModule_IndistinguishableInComposedSet()
    {
        // An auto-activated module appears in the composed set exactly like a hard-required
        // one — same snapshot, no separate lifecycle marker. It is also present in the delta since it
        // is new over the (empty) parent chain.
        var integration = Identification.Create(5, 5, 1);
        var registry = Registry(
            Mod(Vulkan),
            Integration(integration, [Vulkan]));
        var state = new StateComposition(StateId, Identification.Empty, [Vulkan]);

        var result = StateComposer.Compose(state, Seed(), registry);

        await Assert.That(result.ComposedSet.SetEquals(Seed(Vulkan, integration))).IsTrue();
        await Assert.That(result.Delta.ToHashSet().Contains(integration)).IsTrue();
    }
}
