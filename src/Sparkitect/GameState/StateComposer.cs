using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// A module's direct composition declaration: its own identity plus the modules it directly requires
/// and the targets it auto-activates with. Plain <see cref="Identification"/>-keyed data — the composer
/// depends on no state/module contract type, so it is unit-testable without a full GameStateManager.
/// </summary>
/// <param name="Id">The module's identity.</param>
/// <param name="Requires">Modules this module directly requires (transitively closed by the composer).</param>
/// <param name="ActivatesWith">
/// Targets that auto-activate this module: when ALL are present in a state's resolved set, this module
/// (and its own Requires closure) auto-composes into that set.
/// </param>
internal sealed record ModuleComposition(
    Identification Id,
    IReadOnlyList<Identification> Requires,
    IReadOnlyList<Identification> ActivatesWith);

/// <summary>
/// A state's direct composition declaration: its identity, its parent, and the modules it directly
/// declares. The parent-chain composed set is supplied to the composer as an ambient seed.
/// </summary>
/// <param name="Id">The state's identity.</param>
/// <param name="ParentId">The parent state's identity (<see cref="Identification.Empty"/> for the root anchor).</param>
/// <param name="DirectModules">Modules the state declares directly (transitively closed by the composer).</param>
internal sealed record StateComposition(
    Identification Id,
    Identification ParentId,
    IReadOnlyList<Identification> DirectModules);

/// <summary>
/// The composer's output for a single state: the complete authoritative module set (source of truth)
/// and the container-layering delta (set minus the parent-chain seed).
/// </summary>
/// <param name="Id">The state's identity.</param>
/// <param name="ComposedSet">The complete resolved module set (order carries no contract).</param>
/// <param name="Delta">The modules this state adds over its parent chain (container-build bookkeeping).</param>
internal sealed record ComposedState(
    Identification Id,
    IReadOnlySet<Identification> ComposedSet,
    IReadOnlyList<Identification> Delta);

/// <summary>
/// Pure composition unit: resolves a state's complete composed module set from direct declarations.
/// Seeds with the parent-chain composed set (ambient — Core arrives here, never from a Requires list),
/// adds the state's direct modules and their transitive Requires closure, runs the ActivatesWith
/// fixpoint auto-activation pass, and fails loud (naming the resolution chain) when a required or
/// activation-target module is unregistered. Set-semantic and framework-free: a plain visited-set walk,
/// no ordering core, no graph library. Cycles in Requires are harmless.
/// </summary>
internal static class StateComposer
{
    /// <summary>
    /// Resolves the complete composed module set for <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The state's direct composition declaration.</param>
    /// <param name="parentChainComposedSet">
    /// The parent chain's already-composed set (ambient seed). For the root anchor this is empty;
    /// for a child it is the parent's complete composed set. Seed members are treated as present and
    /// are never re-resolved, so they need not appear in <paramref name="registeredModules"/>.
    /// </param>
    /// <param name="registeredModules">All registered modules, keyed by identity.</param>
    /// <returns>The complete composed set plus the parent-chain delta.</returns>
    /// <exception cref="InvalidOperationException">
    /// A directly-declared, transitively-required, or activation-pulled module is not registered.
    /// The message names the ordered resolution chain from the state to the missing module.
    /// </exception>
    public static ComposedState Compose(
        StateComposition state,
        IReadOnlySet<Identification> parentChainComposedSet,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        var set = new HashSet<Identification>(parentChainComposedSet);

        // Hard-Requires closure over the state's direct modules.
        foreach (var moduleId in state.DirectModules)
        {
            ResolveClosure(moduleId, [state.Id], set, registeredModules);
        }

        // ActivatesWith fixpoint: monotone growth over the finite registered-module universe.
        RunActivationFixpoint(state.Id, set, registeredModules);

        var delta = new List<Identification>();
        foreach (var moduleId in set)
        {
            if (!parentChainComposedSet.Contains(moduleId))
            {
                delta.Add(moduleId);
            }
        }

        return new ComposedState(state.Id, set, delta);
    }

    /// <summary>
    /// Adds <paramref name="target"/> and its transitive Requires closure to <paramref name="set"/>.
    /// Terminates on the visited set (present members short-circuit), so cycles are harmless
    /// and ambient seed members are never re-resolved. Threads <paramref name="chain"/> as the ordered
    /// path (state -> intermediate modules) so a missing module throws naming the full chain.
    /// </summary>
    private static void ResolveClosure(
        Identification target,
        IReadOnlyList<Identification> chain,
        HashSet<Identification> set,
        IReadOnlyDictionary<Identification, ModuleComposition> registeredModules)
    {
        // Already present (seed member, prior visit, or cycle) — nothing to resolve.
        if (!set.Add(target))
        {
            return;
        }

        if (!registeredModules.TryGetValue(target, out var module))
        {
            var resolutionChain = chain.Append(target);
            throw new InvalidOperationException(
                $"Module {target} is required but not registered. " +
                $"Resolution chain: {string.Join(" -> ", resolutionChain)}");
        }

        var nextChain = new List<Identification>(chain) { target };
        foreach (var required in module.Requires)
        {
            ResolveClosure(required, nextChain, set, registeredModules);
        }
    }

    /// <summary>
    /// Repeatedly auto-activates any registered module whose ActivatesWith targets are all present,
    /// pulling in the module and its Requires closure, until the set stops growing. Monotone (only ever
    /// adds) over a finite module universe, so it converges; cascades are supported (a module activated
    /// in one pass can satisfy another module's targets in a later pass).
    /// </summary>
    private static void RunActivationFixpoint(
        Identification stateId,
        HashSet<Identification> set,
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
                    ResolveClosure(module.Id, [stateId], set, registeredModules);
                    grew = true;
                }
            }
        }
        while (grew);
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
