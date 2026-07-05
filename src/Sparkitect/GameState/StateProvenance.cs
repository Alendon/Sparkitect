using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// How a module entered a state's composed set, derived by replaying the composer's resolution.
/// </summary>
internal enum ModuleOrigin
{
    /// <summary>Declared directly by the state (present in its DirectModules).</summary>
    AddedDirect,

    /// <summary>Reached only through another composed module's Requires closure.</summary>
    AddedTransitive,

    /// <summary>Present in the parent chain's composed set (inherited as the ambient seed).</summary>
    InheritedFromParent,

    /// <summary>Pulled by the ActivatesWith fixpoint (its activation targets were all present).</summary>
    AutoActivated,
}

/// <summary>
/// A single module's derived provenance within a state's composed set: its <see cref="ModuleOrigin"/>
/// plus the one-hop requirers (composed modules that directly list it in their Requires).
/// </summary>
/// <param name="Id">The module's identity.</param>
/// <param name="Origin">How the module entered the composed set.</param>
/// <param name="Requirers">Composed modules that directly require this module (one hop, no chains).</param>
internal sealed record ModuleProvenance(
    Identification Id,
    ModuleOrigin Origin,
    IReadOnlyList<Identification> Requirers);

/// <summary>
/// A single active state frame's runtime StatelessFunction identities (D-05/D-22): the per-frame,
/// transition-enter, and transition-exit SFs the debuggee's scheduling actually holds for that frame —
/// mod contributions included, since these are the graph-resolved wrapper instances, not a static PSI
/// enumeration. Raw <see cref="Identification"/>s; string-coordinate resolution for reverse-lookup
/// navigation is the snapshot builder's job (later plan), not this seam's.
/// </summary>
/// <param name="StateId">The frame's state identity.</param>
/// <param name="PerFrame">Per-frame SF identities.</param>
/// <param name="TransitionEnter">Transition-enter SF identities.</param>
/// <param name="TransitionExit">Transition-exit SF identities.</param>
internal sealed record FrameStatelessFunctions(
    Identification StateId,
    IReadOnlyList<Identification> PerFrame,
    IReadOnlyList<Identification> TransitionEnter,
    IReadOnlyList<Identification> TransitionExit);

/// <summary>
/// Debug-information seam (D-18/D-22): re-derives each module's origin badge and one-hop requirers for a
/// state's complete composed set by replaying the <see cref="StateComposer"/>'s closure + ActivatesWith
/// fixpoint over the same plain inputs — without mutating the shipped pure static composer, which retains
/// no origin on success. The set it resolves is identical to the composer's <c>ComposedSet</c> by
/// construction; only the origin annotation is added. Kept an explicit in-process debug seam, never
/// promoted onto the public <c>IGameStateManager.StateStack</c>.
/// </summary>
internal static class StateProvenance
{
    /// <summary>
    /// Derives per-module provenance for <paramref name="state"/>'s complete composed set.
    /// </summary>
    /// <param name="state">The state's direct composition declaration.</param>
    /// <param name="parentChainComposedSet">The parent chain's composed set (ambient seed — inherited).</param>
    /// <param name="registeredModules">All registered modules keyed by identity.</param>
    /// <returns>One <see cref="ModuleProvenance"/> per module in the resolved composed set.</returns>
    public static IReadOnlyList<ModuleProvenance> Derive(
        StateComposition state,
        IReadOnlySet<Identification> parentChainComposedSet,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        // Mirror StateComposer.Compose: seed with the inherited parent chain, close over direct modules,
        // then run the ActivatesWith fixpoint — recording each newly-resolved module's origin as it is
        // first reached (visit order matches the composer, so the resolved set is identical).
        var set = new HashSet<Identification>(parentChainComposedSet);
        var origin = new Dictionary<Identification, ModuleOrigin>();
        foreach (var seedMember in parentChainComposedSet)
        {
            origin[seedMember] = ModuleOrigin.InheritedFromParent;
        }

        foreach (var moduleId in state.DirectModules)
        {
            ResolveClosure(moduleId, ModuleOrigin.AddedDirect, set, origin, registeredModules);
        }

        RunActivationFixpoint(set, origin, registeredModules);

        // One-hop requirers, from the final set: composed modules that directly list each module in Requires.
        return set
            .Select(id => new ModuleProvenance(id, origin[id], OneHopRequirers(id, set, registeredModules)))
            .ToList();
    }

    // Adds target (if new) with the given origin, then recurses its Requires closure as AddedTransitive.
    // Members already present (inherited seed, prior visit, cycle) short-circuit and keep their origin —
    // so inherited-from-parent always wins over a re-declaration, matching the composer's visited-set.
    private static void ResolveClosure(
        Identification target,
        ModuleOrigin rootOrigin,
        HashSet<Identification> set,
        Dictionary<Identification, ModuleOrigin> origin,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        if (!set.Add(target))
        {
            return;
        }

        origin[target] = rootOrigin;

        // Unregistered targets cannot occur here (provenance is derived only for an already-validated
        // compose); tolerate rather than replay the composer's fail-loud throw.
        if (!registeredModules.TryGetValue(target, out var module))
        {
            return;
        }

        foreach (var required in module.Requires)
        {
            ResolveClosure(required, ModuleOrigin.AddedTransitive, set, origin, registeredModules);
        }
    }

    // Fixpoint auto-activation, mirroring StateComposer.RunActivationFixpoint: the activated module itself
    // is AutoActivated; anything pulled through its Requires closure is AddedTransitive.
    private static void RunActivationFixpoint(
        HashSet<Identification> set,
        Dictionary<Identification, ModuleOrigin> origin,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        bool grew;
        do
        {
            grew = false;
            foreach (var module in registeredModules.Values)
            {
                if (module.ActivatesWith.Count == 0 || set.Contains(module.Id))
                {
                    continue;
                }

                if (AllPresent(module.ActivatesWith, set))
                {
                    ResolveClosure(module.Id, ModuleOrigin.AutoActivated, set, origin, registeredModules);
                    grew = true;
                }
            }
        }
        while (grew);
    }

    private static IReadOnlyList<Identification> OneHopRequirers(
        Identification target,
        HashSet<Identification> set,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        var requirers = new List<Identification>();
        foreach (var candidate in set)
        {
            if (registeredModules.TryGetValue(candidate, out var module) && module.Requires.Contains(target))
            {
                requirers.Add(candidate);
            }
        }

        return requirers;
    }

    private static bool AllPresent(IReadOnlyList<Identification> targets, HashSet<Identification> set)
    {
        foreach (var target in targets)
        {
            if (!set.Contains(target))
            {
                return false;
            }
        }

        return true;
    }
}
