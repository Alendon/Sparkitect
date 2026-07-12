using Sparkitect.Debug.Protocol;
using Sparkitect.GameState;
using Sparkitect.Modding;
using WireOrigin = Sparkitect.Debug.Protocol.ModuleOrigin;
using EngineOrigin = Sparkitect.GameState.ModuleOrigin;

namespace Sparkitect.Debug;

/// <summary>
/// Projects the running game state manager onto the debug channel's wire snapshot: per active frame, the
/// COMPLETE composed module set (never the delta) annotated with origin badges + one-hop requirers from the
/// provenance seam, the per-frame runtime StatelessFunction sets from the SF seam, the mods added at the
/// frame, and summary counts — every navigable id resolved numeric to a string mod/category/item triple via
/// <see cref="IIdentificationManager"/>. The channel host publishes the result. Static: a stateless
/// projection over the manager's read-seams (the debug-information helper style).
/// </summary>
internal static class DebugSnapshotBuilder
{
    /// <summary>
    /// The debug-channel wire protocol version marker. Bumped whenever the snapshot data design
    /// changes so a mismatched plugin degrades loudly rather than half-rendering.
    /// </summary>
    public const int ProtocolVersion = 1;

    /// <summary>
    /// Builds the current <see cref="DebugSnapshot"/> from <paramref name="gameStateManager"/>, resolving
    /// every id to a string triple via <paramref name="identifications"/>. The channel host calls this on
    /// connect and on every composition change.
    /// </summary>
    public static DebugSnapshot Build(
        IGameStateManager gameStateManager, IIdentificationManager identifications)
    {
        // Reach the internal provenance + per-frame SF read-seams (the SettingsModule downcast pattern);
        // neither is promoted onto the public IGameStateManager.
        if (gameStateManager is not GameStateManager gsm)
        {
            throw new InvalidOperationException(
                $"DebugSnapshotBuilder requires the concrete {nameof(GameStateManager)} to reach the debug read-seams.");
        }

        // Per-frame added-mods come from the public stack projection, indexed by state so the SF spine joins.
        var addedModsByState = gsm.StateStack.ToDictionary(e => e.StateId, e => e.AddedMods);

        // The per-frame SF accessor is the render spine: top-of-stack first, runtime SF truth.
        var frames = gsm.GetFrameStatelessFunctions()
            .Select(frame => BuildFrame(frame, gsm, identifications, addedModsByState))
            .ToList();

        return new DebugSnapshot(ProtocolVersion, frames);
    }

    private static StateFrame BuildFrame(
        FrameStatelessFunctions frame,
        GameStateManager gsm,
        IIdentificationManager ids,
        IReadOnlyDictionary<Identification, IReadOnlyList<string>> addedModsByState)
    {
        // Modules = the COMPLETE composed set with origin badges + one-hop requirers, never the
        // delta — a child frame shows every inherited module.
        var modules = gsm.GetStateProvenance(frame.StateId)
            .Select(p => new ModuleEntry(
                Resolve(p.Id, ids),
                ToWireOrigin(p.Origin),
                p.Requirers.Select(r => Resolve(r, ids)).ToList()))
            .ToList();

        var statelessFunctions = new List<StatelessFunctionEntry>();
        AddSfs(statelessFunctions, frame.PerFrame, StatelessFunctionKind.PerFrame, ids);
        AddSfs(statelessFunctions, frame.TransitionEnter, StatelessFunctionKind.TransitionEnter, ids);
        AddSfs(statelessFunctions, frame.TransitionExit, StatelessFunctionKind.TransitionExit, ids);

        var addedMods = addedModsByState.TryGetValue(frame.StateId, out var mods)
            ? mods.ToList()
            : [];

        // Summary counts feed the enriched header: module set size + mods-added count.
        return new StateFrame(
            Resolve(frame.StateId, ids),
            modules,
            statelessFunctions,
            addedMods,
            modules.Count,
            addedMods.Count);
    }

    private static void AddSfs(
        List<StatelessFunctionEntry> sink,
        IReadOnlyList<Identification> ids,
        StatelessFunctionKind kind,
        IIdentificationManager identifications)
    {
        foreach (var id in ids)
        {
            sink.Add(new StatelessFunctionEntry(Resolve(id, identifications), kind));
        }
    }

    // Resolves a runtime numeric Identification to its string mod/category/item triple. Fails
    // loud when unresolvable — a navigable row without a resolvable string id is a broken snapshot, not a
    // silently-emptied one.
    private static IdName Resolve(Identification id, IIdentificationManager identifications)
    {
        if (!identifications.TryResolveIdentification(id, out var mod, out var category, out var item))
        {
            throw new InvalidOperationException(
                $"Debug snapshot could not resolve identification {id} to a string (mod/category/item) triple; " +
                "every navigable row requires a resolvable string id.");
        }

        return new IdName(mod!, category!, item!);
    }

    private static WireOrigin ToWireOrigin(EngineOrigin origin) => origin switch
    {
        EngineOrigin.AddedDirect => WireOrigin.AddedDirect,
        EngineOrigin.AddedTransitive => WireOrigin.AddedTransitive,
        EngineOrigin.InheritedFromParent => WireOrigin.InheritedFromParent,
        EngineOrigin.AutoActivated => WireOrigin.AutoActivatedIntegration,
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown module origin."),
    };
}
