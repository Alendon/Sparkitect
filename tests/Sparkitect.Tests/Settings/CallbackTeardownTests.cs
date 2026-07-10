using System.Reflection;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Stateless;

namespace Sparkitect.Tests.Settings;

/// <summary>
/// Pins frame-scoped callback teardown: a subscription must not outlive the frame it was made in.
///
/// The full end-to-end path (real GameStateManager: enter root → push child → subscribe → pop child →
/// teardown fires) is not drivable as a unit test — <see cref="GameStateManager.EnterRootState"/> loads
/// mods from disk, requires an IEntryStateSelector, and then blocks forever in StartMainLoop, and
/// CreateStateFrame is private and depends on the generated module ids that only populate after a full
/// registry bootstrap. Per the plan's documented fallback, this suite instead drives the real scheduling
/// component the runtime uses — <see cref="OnFrameExitScheduling.BuildGraph"/> — reaching the teardown
/// function ONLY via the ambient (Root anchor) gate, and pins the root-load wiring and the teardown
/// function structurally. Together these fail if either the Task 2 ambient enhancement or the Task 3
/// root-load/teardown-function were reverted.
/// </summary>
public class CallbackTeardownTests
{
    private static readonly Identification SettingsOwnerModule = Identification.Create(900, 1, 1);
    private static readonly Identification ChildOnlyModule = Identification.Create(900, 1, 2);
    private static readonly Identification ChildStateId = Identification.Create(900, 2, 1);
    private static readonly Identification TeardownFunctionId = Identification.Create(900, 3, 1);

    private static readonly Identification<bool> Flag = new(Identification.Create(901, 1, 1));
    private static readonly Identification UserSourceId = Identification.Create(901, 2, 1);

    // ── Task 2: the ambient (Root anchor) gate is what admits a root-only module's frame-exit function ──

    [Test]
    public async Task ExitFunction_ReachedViaAmbientModule_EntersExitGraph()
    {
        // Child frame declares ChildOnlyModule — NOT the owner module. The owner is reached ONLY through
        // AmbientModules (the never-framed Root anchor's set), exactly as a root-loaded settings module is.
        var context = new TransitionContext
        {
            StateStack = [((IReadOnlyList<Identification>)[ChildOnlyModule], ChildStateId)],
            IsEnterTransition = false,
            AmbientModules = [SettingsOwnerModule]
        };

        var builder = new ExecutionGraphBuilder();
        var scheduling = new OnFrameExitScheduling([], []) { OwnerId = new FixedLazyIdentification(SettingsOwnerModule) };
        scheduling.BuildGraph(builder, context, TeardownFunctionId);

        // The teardown function entered the exit-transition graph purely via the ambient gate.
        await Assert.That(builder.Resolve()).Contains(TeardownFunctionId);
    }

    [Test]
    public async Task ExitFunction_WithoutAmbientVisibility_ExcludedFromExitGraph()
    {
        // Same child frame, but the owner is neither in the stack nor ambient — reverting the Task 2
        // ambient OR in IsModuleLoaded collapses to exactly this state, so the function must be excluded.
        var context = new TransitionContext
        {
            StateStack = [((IReadOnlyList<Identification>)[ChildOnlyModule], ChildStateId)],
            IsEnterTransition = false,
            AmbientModules = []
        };

        var builder = new ExecutionGraphBuilder();
        var scheduling = new OnFrameExitScheduling([], []) { OwnerId = new FixedLazyIdentification(SettingsOwnerModule) };
        scheduling.BuildGraph(builder, context, TeardownFunctionId);

        await Assert.That(builder.Resolve()).DoesNotContain(TeardownFunctionId);
    }

    // ── Task 3: the settings modules are root-loaded and the teardown function is a real exit function ──

    [Test]
    public async Task RootDescriptor_OverridesDirectModules()
    {
        // RootGameStateDescriptor must override DirectModules to declare root-loaded modules (Core,
        // Settings, UserSettings). We verify the override exists structurally — the actual Identification
        // values require a bootstrapped IdentificationManager and are pinned by the GSM integration tests.
        var prop = typeof(RootGameStateDescriptor).GetProperty(
            nameof(RootGameStateDescriptor.DirectModules),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        await Assert.That(prop).IsNotNull();
        await Assert.That(prop!.DeclaringType).IsEqualTo(typeof(RootGameStateDescriptor));
    }

    [Test]
    public async Task SettingsModule_DeclaresFrameExitTeardownFunction()
    {
        var teardown = typeof(SettingsModule)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(m =>
                m.GetCustomAttribute<OnFrameExitSchedulingAttribute>() is not null &&
                m.GetCustomAttribute<TransitionFunctionAttribute>()?.Identifier == "clear_settings_subscriptions");

        await Assert.That(teardown).IsNotNull();
    }

    // ── Behavioral: the teardown clears the exiting frame's bag while the parent frame's survives ──

    [Test]
    public async Task FrameTeardown_ClearsExitingFrameSubscription_ParentSubscriptionSurvives()
    {
        var manager = new SettingsManager();
        var parentToken = new object();
        var childToken = new object();

        // The provider returns whichever frame is currently the leaf, mirroring the runtime wiring
        // (UseFrameTokenProvider(() => gsm.CurrentCoreContainer)).
        object currentToken = parentToken;
        manager.UseFrameTokenProvider(() => currentToken);

        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        var parentFires = 0;
        manager.Subscribe<bool>(Flag, _ => parentFires++);

        // Enter a child frame and subscribe within it.
        currentToken = childToken;
        var childFires = 0;
        manager.Subscribe<bool>(Flag, _ => childFires++);

        // Child frame exits: the frame-exit StateFunction clears that frame's subscriptions.
        manager.ClearSubscriptionsForFrame(childToken);

        // A subsequent effective-value change must reach the surviving parent callback only.
        manager.Set(Flag, UserSourceId, true);

        await Assert.That(childFires).IsEqualTo(0);
        await Assert.That(parentFires).IsEqualTo(1);
    }

    // Test double for ILazyIdentification: resolves to a fixed value, never throws.
    private readonly record struct FixedLazyIdentification(Identification Value) : ILazyIdentification
    {
        public Identification Resolve() => Value;
    }
}
