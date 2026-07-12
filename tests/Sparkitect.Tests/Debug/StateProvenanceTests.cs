using System.Runtime.CompilerServices;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Tests.Debug;

/// <summary>
/// Pins the provenance re-derivation OUTCOME against a hand-built module graph: each of the four
/// origin badges and the one-hop requirer edge. Tests the derived origin, never the composer's internals or
/// any log side effect. Ids are synthetic (module ids are runtime-assigned; the pure derivation is
/// id-agnostic under set semantics, mirroring the composer).
/// </summary>
public class StateProvenanceTests
{
    private static readonly Identification Core = Identification.Create(2, 1, 50);
    private static readonly Identification Inherited = Identification.Create(2, 1, 62);
    private static readonly Identification Direct = Identification.Create(2, 1, 60);
    private static readonly Identification Transitive = Identification.Create(2, 1, 61);
    private static readonly Identification Auto = Identification.Create(2, 1, 63);

    // Direct requires Transitive; Auto activates on Core (inherited via the parent chain seed).
    private static Dictionary<Identification, ModuleComposition> BuildUniverse() => new()
    {
        [Core] = new(Core, [], []),
        [Direct] = new(Direct, [Transitive], []),
        [Transitive] = new(Transitive, [], []),
        [Auto] = new(Auto, [], [Core]),
    };

    // Parent chain already composed { Inherited, Core }; the state declares Direct.
    private static IReadOnlyList<ModuleProvenance> Derive()
    {
        var state = new StateComposition(
            Identification.Create(2, 1, 70), Identification.Create(2, 1, 99), [Direct]);
        var parentChain = new HashSet<Identification> { Inherited, Core };
        return StateProvenance.Derive(state, parentChain, BuildUniverse());
    }

    private static ModuleProvenance For(IReadOnlyList<ModuleProvenance> all, Identification id)
        => all.Single(p => p.Id == id);

    [Test]
    public async Task DirectlyDeclaredModule_IsAddedDirect()
    {
        var p = For(Derive(), Direct);
        await Assert.That(p.Origin).IsEqualTo(ModuleOrigin.AddedDirect);
    }

    [Test]
    public async Task RequiredOnlyModule_IsAddedTransitive_WithOneHopRequirer()
    {
        var p = For(Derive(), Transitive);
        await Assert.That(p.Origin).IsEqualTo(ModuleOrigin.AddedTransitive);
        await Assert.That(p.Requirers).Contains(Direct);
    }

    [Test]
    public async Task ParentChainModule_IsInheritedFromParent()
    {
        var p = For(Derive(), Inherited);
        await Assert.That(p.Origin).IsEqualTo(ModuleOrigin.InheritedFromParent);
    }

    [Test]
    public async Task FixpointPulledModule_IsAutoActivated()
    {
        var p = For(Derive(), Auto);
        await Assert.That(p.Origin).IsEqualTo(ModuleOrigin.AutoActivated);
    }

    [Test]
    public async Task DerivedSet_MatchesTheComposerResolvedSet()
    {
        // Re-derivation must resolve exactly the composer's ComposedSet (only the origin annotation is new).
        var state = new StateComposition(
            Identification.Create(2, 1, 70), Identification.Create(2, 1, 99), [Direct]);
        var parentChain = new HashSet<Identification> { Inherited, Core };
        var universe = BuildUniverse();

        var composed = StateComposer.Compose(state, parentChain, universe);
        var derived = StateProvenance.Derive(state, parentChain, universe);

        await Assert.That(derived.Select(p => p.Id).ToHashSet().SetEquals(composed.ComposedSet)).IsTrue();
    }
}

/// <summary>
/// Pins the per-frame StatelessFunction accessor OUTCOME: each frame's PerFrame/TransitionEnter/
/// TransitionExit SF identities surface top-of-stack first, and a mod-contributed SF present in a frame's
/// runtime wrappers appears. Frames are hand-built with known SFs and pushed onto the private stack via
/// <see cref="UnsafeAccessor"/> (no reflection); the accessor's own deps are unused so they are left null.
/// </summary>
public class StateProvenanceFrameSfAccessorTests
{
    private static readonly Identification BottomState = Identification.Create(2, 1, 80);
    private static readonly Identification TopState = Identification.Create(2, 1, 81);
    private static readonly Identification PerFrameSf = Identification.Create(2, 1, 90);
    private static readonly Identification ModSf = Identification.Create(2, 1, 91);
    private static readonly Identification EnterSf = Identification.Create(2, 1, 92);
    private static readonly Identification ExitSf = Identification.Create(2, 1, 93);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stateStack")]
    private static extern ref Stack<ActiveStateFrame> StateStackField(GameStateManager gsm);

    private sealed class FakeSf(Identification id) : IStatelessFunction
    {
        public Identification Identification => id;
        public void Execute() { }
        public void Initialize(IResolutionScope scope) { }
    }

    private static ActiveStateFrame Frame(Identification stateId, IReadOnlyList<IStatelessFunction> perFrame)
        => new(stateId, null!, [], [], [new FakeSf(EnterSf)], [new FakeSf(ExitSf)], perFrame);

    private static GameStateManager BuildManagerWithFrames()
    {
        var gsm = new GameStateManager
        {
            ModManager = null!,
            RegistryManager = null!,
            DIService = null!,
            FunctionManager = null!,
        };

        var stack = StateStackField(gsm);
        stack.Push(Frame(BottomState, [new FakeSf(PerFrameSf)]));
        // Top frame carries an extra mod-contributed per-frame SF.
        stack.Push(Frame(TopState, [new FakeSf(PerFrameSf), new FakeSf(ModSf)]));
        return gsm;
    }

    [Test]
    public async Task Accessor_ReturnsPerFrameSfIdentities_TopOfStackFirst()
    {
        var frames = BuildManagerWithFrames().GetFrameStatelessFunctions();

        await Assert.That(frames[0].StateId).IsEqualTo(TopState);
        await Assert.That(frames[1].StateId).IsEqualTo(BottomState);
        await Assert.That(frames[0].PerFrame).Contains(PerFrameSf);
        await Assert.That(frames[0].TransitionEnter).Contains(EnterSf);
        await Assert.That(frames[0].TransitionExit).Contains(ExitSf);
    }

    [Test]
    public async Task ModContributedSf_SurfacesInAccessorOutput()
    {
        var frames = BuildManagerWithFrames().GetFrameStatelessFunctions();

        await Assert.That(frames[0].PerFrame).Contains(ModSf);
    }
}
