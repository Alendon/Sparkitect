using System.Runtime.CompilerServices;
using Sparkitect.Debug;
using Sparkitect.Debug.Protocol;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Stateless;
using WireOrigin = Sparkitect.Debug.Protocol.ModuleOrigin;

namespace Sparkitect.Tests.Debug;

/// <summary>
/// Pins the D-22/D-11 snapshot-builder OUTCOME against a hand-built composed stack: the per-frame Modules
/// section is the COMPLETE composed set (a child frame shows inherited modules, Pitfall 4), each module
/// carries its origin badge + one-hop requirers, per-frame SF sets are present with the right kinds, every
/// navigable id is a resolved string triple, and the summary counts + version marker are set. Tests the
/// produced snapshot, never registrations or log side effects.
/// </summary>
public class DebugSnapshotBuilderTests
{
    // Real IdentificationManager so ids are runtime-assigned AND reverse-resolvable to their string triples.
    // RegisterObject does not auto-register the mod/category, so seed them first (else ids come back Empty).
    private static readonly IdentificationManager Ids = CreateIds();

    private static IdentificationManager CreateIds()
    {
        var manager = new IdentificationManager();
        manager.RegisterMod("test_mod");
        manager.RegisterCategory("module");
        manager.RegisterCategory("state");
        manager.RegisterCategory("sf");
        return manager;
    }

    private static readonly Identification Core = Ids.RegisterObject("test_mod", "module", "core");
    private static readonly Identification Inherited = Ids.RegisterObject("test_mod", "module", "inherited");
    private static readonly Identification Direct = Ids.RegisterObject("test_mod", "module", "direct");
    private static readonly Identification Transitive = Ids.RegisterObject("test_mod", "module", "transitive");
    private static readonly Identification Auto = Ids.RegisterObject("test_mod", "module", "auto");

    private static readonly Identification ParentState = Ids.RegisterObject("test_mod", "state", "parent");
    private static readonly Identification ChildState = Ids.RegisterObject("test_mod", "state", "child");

    private static readonly Identification PerFrameSf = Ids.RegisterObject("test_mod", "sf", "per_frame");
    private static readonly Identification EnterSf = Ids.RegisterObject("test_mod", "sf", "enter");
    private static readonly Identification ExitSf = Ids.RegisterObject("test_mod", "sf", "exit");

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_registeredStates")]
    private static extern ref Dictionary<Identification, StateMetadata> RegisteredStates(GameStateManager gsm);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_registeredModules")]
    private static extern ref Dictionary<Identification, ModuleMetadata> RegisteredModules(GameStateManager gsm);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stateDirectModules")]
    private static extern ref Dictionary<Identification, IReadOnlyList<Identification>> StateDirectModules(GameStateManager gsm);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stateStack")]
    private static extern ref Stack<ActiveStateFrame> StateStack(GameStateManager gsm);

    private sealed class FakeSf(Identification id) : IStatelessFunction
    {
        public Identification Identification => id;
        public void Execute() { }
        public void Initialize(IResolutionScope scope) { }
    }

    // Child composes { Core, Inherited (both inherited from parent), Direct, Transitive (required by Direct),
    // Auto (activates on Core) }; parent composes { Core, Inherited }.
    private static GameStateManager BuildManager()
    {
        var gsm = new GameStateManager
        {
            ModManager = null!,
            RegistryManager = null!,
            DIService = null!,
            FunctionManager = null!,
        };

        var modules = RegisteredModules(gsm);
        modules[Core] = new(Core, [], [], typeof(object));
        modules[Inherited] = new(Inherited, [], [], typeof(object));
        modules[Direct] = new(Direct, [Transitive], [], typeof(object));
        modules[Transitive] = new(Transitive, [], [], typeof(object));
        modules[Auto] = new(Auto, [], [Core], typeof(object));

        var parentComposed = new HashSet<Identification> { Core, Inherited };
        var childComposed = new HashSet<Identification> { Core, Inherited, Direct, Transitive, Auto };

        var states = RegisteredStates(gsm);
        states[ParentState] = new(ParentState, Identification.Empty, [Core, Inherited], parentComposed, typeof(object));
        states[ChildState] = new(ChildState, ParentState, [Direct], childComposed, typeof(object));

        var direct = StateDirectModules(gsm);
        direct[ParentState] = [Core, Inherited];
        direct[ChildState] = [Direct];

        var stack = StateStack(gsm);
        stack.Push(ParentFrame());
        stack.Push(ChildFrame());
        return gsm;
    }

    private static ActiveStateFrame ParentFrame()
        => new(ParentState, null!, [], [Core, Inherited], [], [], [new FakeSf(PerFrameSf)]);

    private static ActiveStateFrame ChildFrame()
        => new(ChildState, null!, ["child_mod"], [Direct],
            [new FakeSf(EnterSf)], [new FakeSf(ExitSf)], [new FakeSf(PerFrameSf)]);

    private static DebugSnapshot Snapshot() => DebugSnapshotBuilder.Build(BuildManager(), Ids);

    private static ModuleEntry Module(StateFrame frame, Identification id)
    {
        Ids.TryResolveIdentification(id, out var mod, out var cat, out var item);
        return frame.Modules.Single(m => m.Id.Mod == mod && m.Id.Category == cat && m.Id.Item == item);
    }

    [Test]
    public async Task Frames_AreTopOfStackFirst_WithVersionMarker()
    {
        var snapshot = Snapshot();
        await Assert.That(snapshot.ProtocolVersion).IsEqualTo(DebugSnapshotBuilder.ProtocolVersion);
        await Assert.That(snapshot.Frames.Count).IsEqualTo(2);
        await Assert.That(snapshot.Frames[0].StateId.Item).IsEqualTo("child");
        await Assert.That(snapshot.Frames[1].StateId.Item).IsEqualTo("parent");
    }

    [Test]
    public async Task ChildFrame_Modules_AreTheCompleteComposedSet_NotTheDelta()
    {
        // Pitfall 4: the child frame shows ALL inherited modules, not just its delta [Direct].
        var child = Snapshot().Frames[0];
        await Assert.That(child.Modules.Count).IsEqualTo(5);
        await Assert.That(child.ModuleCount).IsEqualTo(5);
        var items = child.Modules.Select(m => m.Id.Item).ToHashSet();
        await Assert.That(items.SetEquals(new[] { "core", "inherited", "direct", "transitive", "auto" })).IsTrue();
    }

    [Test]
    public async Task Modules_CarryOriginBadges_AndOneHopRequirers()
    {
        var child = Snapshot().Frames[0];
        await Assert.That(Module(child, Direct).Origin).IsEqualTo(WireOrigin.AddedDirect);
        await Assert.That(Module(child, Inherited).Origin).IsEqualTo(WireOrigin.InheritedFromParent);
        await Assert.That(Module(child, Auto).Origin).IsEqualTo(WireOrigin.AutoActivatedIntegration);

        var transitive = Module(child, Transitive);
        await Assert.That(transitive.Origin).IsEqualTo(WireOrigin.AddedTransitive);
        await Assert.That(transitive.Requirers.Select(r => r.Item)).Contains("direct");
    }

    [Test]
    public async Task Frame_CarriesPerFrameSfSets_WithKinds()
    {
        var child = Snapshot().Frames[0];
        var perFrame = child.StatelessFunctions.Single(s => s.Kind == StatelessFunctionKind.PerFrame);
        var enter = child.StatelessFunctions.Single(s => s.Kind == StatelessFunctionKind.TransitionEnter);
        var exit = child.StatelessFunctions.Single(s => s.Kind == StatelessFunctionKind.TransitionExit);

        await Assert.That(perFrame.Id.Item).IsEqualTo("per_frame");
        await Assert.That(enter.Id.Item).IsEqualTo("enter");
        await Assert.That(exit.Id.Item).IsEqualTo("exit");
    }

    [Test]
    public async Task AllNavigableIds_AreResolvedStringTriples_AndCountsMatch()
    {
        var child = Snapshot().Frames[0];

        await Assert.That(child.StateId.Mod).IsEqualTo("test_mod");
        await Assert.That(child.AddedMods).Contains("child_mod");
        await Assert.That(child.ModCount).IsEqualTo(1);

        foreach (var module in child.Modules)
        {
            await Assert.That(module.Id.Mod).IsNotEmpty();
            await Assert.That(module.Id.Category).IsNotEmpty();
            await Assert.That(module.Id.Item).IsNotEmpty();
        }
        foreach (var sf in child.StatelessFunctions)
        {
            await Assert.That(sf.Id.Mod).IsNotEmpty();
            await Assert.That(sf.Id.Item).IsNotEmpty();
        }
    }
}
