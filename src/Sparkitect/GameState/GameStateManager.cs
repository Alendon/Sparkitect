using System.Diagnostics;
using Semver;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Debug;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.Modding.IDs;
using Sparkitect.Metadata;
using Sparkitect.Stateless;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

/// <summary>
/// Manages game state transitions, module lifecycle, and main loop execution
/// </summary>
[StateService<IGameStateManager, CoreModule>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade, IGameStateManagerStateFacade,
    IGameStateTransitionSignal
{
    private const string DefaultRootModConfigPath = "mods/roots.json";

    private readonly Dictionary<Identification, StateMetadata> _registeredStates = new();
    private readonly Dictionary<Identification, ModuleMetadata> _registeredModules = new();
    private readonly Stack<ActiveStateFrame> _stateStack = new();

    // Debug-information seam (D-18): the per-state direct module declarations, retained at finalize before
    // StateMetadata.ModuleIds is overwritten with the container-layering delta. Provenance re-derivation
    // (StateProvenance) needs the direct declarations to distinguish added-direct from added-transitive;
    // the composer discards them. Read only via the internal debug accessor (downcast), never on the public API.
    private readonly Dictionary<Identification, IReadOnlyList<Identification>> _stateDirectModules = new();

    public IEnumerable<string> LoadedMods => _stateStack.SelectMany(x => x.AddedMods);

    /// <inheritdoc />
    public IReadOnlyList<StateStackEntry> StateStack =>
        _stateStack
            .Reverse()
            .Select(f => new StateStackEntry(
                f.StateId,
                _registeredStates[f.StateId].ComposedSet.ToList(),
                f.AddedModuleIds,
                f.AddedMods))
            .ToList();

    /// <inheritdoc />
    public bool IsModLoaded(string modId) => LoadedMods.Contains(modId);

    private readonly List<Func<StateMetadata>> _pendingStates = new();
    private readonly List<Func<ModuleMetadata>> _pendingModules = new();

    private Identification? _pendingTransitionTarget;
    private bool _isTransitioning;
    private bool _shutdownRequested;
    private GsmTransitionDirection _transitionDirection = GsmTransitionDirection.None;

    public required IModManager ModManager { get; init; }
    public required IRegistryManager RegistryManager { get; init; }
    public required IDIService DIService { get; init; }
    public required IStatelessFunctionManager FunctionManager { get; init; }

    GsmTransitionDirection IGameStateTransitionSignal.TransitionDirection => _transitionDirection;

    private IRegistryLifecycleManager RegistryLifecycle => (IRegistryLifecycleManager)RegistryManager;

    public ICoreContainer CurrentCoreContainer => _stateStack.Count > 0 ? _stateStack.Peek().Container : RootContainer;
    public ICoreContainer RootContainer { get; set; } = null!;

    /// <summary>
    /// Enter the Root Game State. To be called by the Engine Bootstrapper
    /// </summary>
    public void EnterRootState()
    {
        Log.Information("Entering root state");

        // Select and load root mods using config file or fallback logic
        var rootMods = SelectRootMods();
        if (rootMods.All(x => x.Id != Constants.VirtualSparkitectModId))
        {
            rootMods =
            [
                new ModFileIdentifier(Constants.VirtualSparkitectModId,
                    SemVersion.Parse(Constants.VirtualSparkitectVersion)),
                ..rootMods
            ];
        }

        Log.Information("Loading {ModCount} root mods", rootMods.Length);
        ModManager.LoadMods(rootMods);

        // Extract mod IDs for registry processing and DI (runtime identification)
        var rootModIds = rootMods.Select(m => m.Id).ToArray();

        // CoreModule owns the module/state/per-frame/transition registries. The manager auto-adds and
        // populates them for the root mods before the finalize pass, which needs those registrations to
        // validate the state/module hierarchy.
        RegistryLifecycle.BootstrapRootRegistries(rootModIds, RootContainer);

        // Finalize registrations
        FinalizeRegistrations();

        // Query entry state selector to determine initial active state
        // Note: Root state is registered but never framed - it's a semantic anchor
        using var entrySelectorContainer = DIService.CreateEntrypointContainer<IEntryStateSelector>(rootModIds);
        var entrySelector = entrySelectorContainer.ResolveMany().FirstOrDefault();

        if (entrySelector == null)
        {
            throw new InvalidOperationException("No EntryStateSelector found in loaded mods");
        }

        var entryStateId = entrySelector.SelectEntryState(RootContainer);

        if (!_registeredStates.ContainsKey(entryStateId))
        {
            throw new InvalidOperationException($"Entry state {entryStateId} selected by EntryStateSelector is not registered");
        }

        Log.Information("Selected entry state: {StateId}", entryStateId);

        // Create entry state frame (not Root - Root is never framed)
        var entryFrame = CreateStateFrame(entryStateId, RootContainer, rootModIds);

        PushState(entryFrame);
        EnterFrameRegistries(entryFrame);

        // Execute entry sequence
        ExecuteEnterMethods(entryFrame.TransitionEnterMethods);

        Log.Information("Entry state active, starting main loop");
        StartMainLoop();
    }

    public void Request(Identification stateId)
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot transition from empty stack");
        }

        var currentStateId = _stateStack.Peek().StateId;

        // Prevent transitioning to Root state (it's never framed)
        var rootStateId = StateID.Sparkitect.Root;
        if (stateId.Equals(rootStateId))
        {
            throw new InvalidOperationException(
                "Cannot transition to Root state. Root is a semantic anchor and cannot be an active state.");
        }

        // Validate transition is parent or child
        if (!_registeredStates.TryGetValue(stateId, out var targetState))
        {
            throw new InvalidOperationException($"Target state {stateId} is not registered");
        }

        if (!_registeredStates.TryGetValue(currentStateId, out var currentState))
        {
            throw new InvalidOperationException($"Current state {currentStateId} is not registered");
        }

        bool isParent = currentState.ParentId.Equals(stateId);
        bool isChild = targetState.ParentId.Equals(currentStateId);

        if (!isParent && !isChild)
        {
            throw new InvalidOperationException(
                $"Transition from {currentStateId} to {stateId} is not allowed. Only parent or immediate child transitions are supported.");
        }

        if (_pendingTransitionTarget.HasValue)
        {
            throw new InvalidOperationException(
                $"A state transition to {_pendingTransitionTarget.Value} is already pending. " +
                $"Cannot request a second transition to {stateId} in the same frame.");
        }

        _pendingTransitionTarget = stateId;
        Log.Debug("Queued state transition to {StateId}", stateId);
    }

    public void RequestWithModChange(ILazyIdentification targetState, IReadOnlyList<ModFileIdentifier> additionalMods)
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot transition from empty stack");
        }

        var currentStateId = _stateStack.Peek().StateId;

        Log.Information("Loading {ModCount} additional mods for state transition", additionalMods.Count);

        // Load the mods (uses ModFileIdentifier for version-specific loading)
        ModManager.LoadMods(additionalMods.ToArray());

        // Extract mod IDs for registry processing (runtime identification)
        var additionalModIds = additionalMods.Select(m => m.Id).ToList();

        // CoreModule's registries are already added; populate them for the newly loaded mods before finalize.
        RegistryLifecycle.ProcessModuleRegistriesForMods(CoreModule.Identification, additionalModIds, CurrentCoreContainer);

        // Finalize registrations (validates state/module hierarchy)
        FinalizeRegistrations();

        // Now compute the target state ID
        var targetStateId = targetState.Resolve();

        // Prevent transitioning to Root state (it's never framed)
        var rootStateId = StateID.Sparkitect.Root;
        if (targetStateId.Equals(rootStateId))
        {
            throw new InvalidOperationException(
                "Cannot transition to Root state. Root is a semantic anchor and cannot be an active state.");
        }

        // Validate target is child of current
        if (!_registeredStates.TryGetValue(targetStateId, out var targetStateDescriptor))
        {
            throw new InvalidOperationException($"Target state {targetStateId} is not registered");
        }

        if (!targetStateDescriptor.ParentId.Equals(currentStateId))
        {
            throw new InvalidOperationException(
                $"RequestWithModChange from {currentStateId} to {targetStateId} is not allowed. Target must be immediate child of current state.");
        }

        // Transition with mod tracking
        _isTransitioning = true;

        try
        {
            Log.Information("Transitioning from {Current} to child {Target} with {ModCount} new mods",
                currentStateId, targetStateId, additionalMods.Count);

            // Parent is currently leaf - execute parent OnFrameExit
            var parentFrame = _stateStack.Peek();
            ExecuteExitMethods(parentFrame.TransitionExitMethods);

            // Create child frame (uses IDs for state frame tracking)
            var parentContainer = _stateStack.Peek().Container;
            var childFrame = CreateStateFrame(targetStateId, parentContainer, additionalModIds);

            // Reconstruct frame with AddedMods (stores IDs for runtime tracking)
            childFrame = childFrame with { AddedMods = additionalModIds };

            PushState(childFrame);
            EnterFrameRegistries(childFrame);

            // Execute child entry sequence
            ExecuteEnterMethods(childFrame.TransitionEnterMethods);

            Log.Information("Transition with mod change complete: now in state {StateId}", childFrame.StateId);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Shutdown()
    {
        Log.Information("Shutdown requested");
        _shutdownRequested = true;
    }

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule, IHasIdentification, new()
    {
        _pendingModules.Add(() =>
        {
            var identification = TStateModule.Identification;
            // Read direct declarations off an ephemeral instance, then discard it (zero reflection).
            var instance = new TStateModule();
            var requiredModules = instance.Requires.ToArray();
            var activatesWith = instance.ActivatesWith.ToArray();
            return new ModuleMetadata(identification, requiredModules, activatesWith, typeof(TStateModule));
        });
    }

    public void RemoveStateModule(Identification id)
    {
        if (_registeredModules.Remove(id))
        {
            Log.Debug("Removed module: {ModuleId}", id);
        }
    }

    // Debug-specific composition-inclusion gate (D-20). Reads the debug_channel setting early — at finalize
    // time the container's CLI/engine-config setting sources are not yet registered, so the ambient manager
    // would see only the default — and drops the debug module from _registeredModules before compose when
    // the channel is off. Not a general setting-gated-module mechanism.
    private void ExcludeDebugModuleWhenDisabled() =>
        DebugModuleGate.ExcludeWhenDisabled(_registeredModules, DebugChannelSettingReader.ReadEnabled());

    // Debug-information seam (D-18/D-22): re-derives per-module origin badges + one-hop requirers for a
    // registered state's complete composed set, replaying the composer over the retained finalize-time
    // inputs (StateProvenance — the shipped static composer is untouched and retains no origin on success).
    // Reached by in-assembly downcast (the SettingsModule pattern); never promoted onto IGameStateManager.
    internal IReadOnlyList<ModuleProvenance> GetStateProvenance(Identification stateId)
    {
        if (!_registeredStates.TryGetValue(stateId, out var metadata))
        {
            throw new InvalidOperationException($"State {stateId} is not registered");
        }

        var directModules = _stateDirectModules.TryGetValue(stateId, out var direct)
            ? direct
            : (IReadOnlyList<Identification>)[];

        // The parent's complete composed set is the ambient seed (agrees with ComposeState); Root seeds empty.
        IReadOnlySet<Identification> parentChainComposedSet = metadata.ParentId.IsEmpty()
            ? new HashSet<Identification>()
            : _registeredStates[metadata.ParentId].ComposedSet;

        var moduleCompositions = _registeredModules.Values.ToDictionary(
            m => m.Id,
            m => new ModuleComposition(m.Id, m.RequiredModules, m.ActivatesWith));

        var declaration = new StateComposition(stateId, metadata.ParentId, directModules);
        return StateProvenance.Derive(declaration, parentChainComposedSet, moduleCompositions);
    }

    // Debug-information seam (D-05/D-22): projects each active frame's cached runtime StatelessFunction
    // wrappers (mod-inclusive graph-resolved instances, not a static PSI enumeration) into per-frame SF
    // identity records, top-of-stack first. Mirrors the StateStack projection shape; reached by in-assembly
    // downcast, never promoted onto IGameStateManager.
    internal IReadOnlyList<FrameStatelessFunctions> GetFrameStatelessFunctions() =>
        // Stack<T> enumerates top-of-stack first (LIFO), which is the D-04 render order.
        _stateStack
            .Select(f => new FrameStatelessFunctions(
                f.StateId,
                f.PerFrameMethods.Select(m => m.Identification).ToList(),
                f.TransitionEnterMethods.Select(m => m.Identification).ToList(),
                f.TransitionExitMethods.Select(m => m.Identification).ToList()))
            .ToList();

    public void AddStateDescriptor<TGameState>(Identification id) where TGameState : class, IGameState, IHasIdentification, new()
    {
        _pendingStates.Add(() =>
        {
            var identification = TGameState.Identification;
            // Read direct declarations off an ephemeral instance, then discard it (zero reflection).
            var instance = new TGameState();
            var parentId = instance.ParentId;
            var modules = instance.DirectModules;
            // Pre-composition record: ModuleIds carries the direct declarations; ComposedSet is populated
            // by FinalizeRegistrations once the parent chain is composed.
            return new StateMetadata(identification, parentId, modules, new HashSet<Identification>(), typeof(TGameState));
        });
    }

    public void RemoveStateDescriptor(Identification id)
    {
        if (_registeredStates.Remove(id))
        {
            Log.Debug("Removed state: {StateId}", id);
        }
    }

    private void FinalizeRegistrations()
    {
        // Register modules
        foreach (var moduleFactory in _pendingModules)
        {
            var metadata = moduleFactory();
            _registeredModules[metadata.Id] = metadata;
            Log.Debug("Finalized module registration: {ModuleId}", metadata.Id);
        }
        _pendingModules.Clear();

        // D-20 composition-inclusion gate — a named seam between the pending-module drain and compose. With
        // the debug channel setting off, drop the debug module from the registered set so its
        // ActivatesWith => [Core] auto-activation never pulls it into any state's composed set (off ⇒
        // absent, not merely inert).
        ExcludeDebugModuleWhenDisabled();

        // Collect state declarations (ModuleIds carries the direct declarations until composition).
        var tempStates = new Dictionary<Identification, StateMetadata>();
        foreach (var stateFactory in _pendingStates)
        {
            var metadata = stateFactory();
            tempStates[metadata.Id] = metadata;
        }
        _pendingStates.Clear();

        // Adapt registered modules into the composer's plain-data inputs.
        var moduleCompositions = _registeredModules.Values.ToDictionary(
            m => m.Id,
            m => new ModuleComposition(m.Id, m.RequiredModules, m.ActivatesWith));

        // Compose every state. The composer owns transitive-Requires closure, ActivatesWith
        // auto-activation, and fail-loud presence validation (naming the resolution chain); parent
        // chains resolve on demand and memoize into composedStates.
        var composedStates = new Dictionary<Identification, ComposedState>();
        foreach (var stateId in tempStates.Keys)
        {
            ComposeState(stateId, tempStates, moduleCompositions, composedStates);
        }

        // Snapshot: the complete composed set is the source of truth; the delta is retained for
        // container layering and registry-lifecycle bookkeeping.
        foreach (var (stateId, tempMetadata) in tempStates)
        {
            var composed = composedStates[stateId];
            // Retain the direct declarations for the debug provenance seam before ModuleIds becomes the delta.
            _stateDirectModules[stateId] = tempMetadata.ModuleIds;
            _registeredStates[stateId] = new StateMetadata(
                tempMetadata.Id,
                tempMetadata.ParentId,
                composed.Delta,
                composed.ComposedSet,
                tempMetadata.DescriptorType);
            Log.Debug("Finalized state registration: {StateId} (parent: {ParentId}, delta: {DeltaCount}, composed: {ComposedCount})",
                stateId, tempMetadata.ParentId, composed.Delta.Count, composed.ComposedSet.Count);
        }

        Log.Information("Finalized {ModuleCount} modules and {StateCount} states",
            _registeredModules.Count, _registeredStates.Count);
    }

    // Composes a state's complete module set via StateComposer, resolving (and memoizing) the parent
    // chain first so the parent's composed set seeds the child. Parent-chain-to-Root validation stays
    // here (GSM concern); the composer owns module presence and closure.
    private ComposedState ComposeState(
        Identification stateId,
        Dictionary<Identification, StateMetadata> tempStates,
        Dictionary<Identification, ModuleComposition> moduleCompositions,
        Dictionary<Identification, ComposedState> composedStates)
    {
        if (composedStates.TryGetValue(stateId, out var already))
        {
            return already;
        }

        var tempMetadata = tempStates[stateId];

        ValidateStateHierarchy(stateId, tempMetadata, tempStates);

        // The parent's complete composed set is the ambient seed. Root (Empty parent) seeds empty;
        // Core arrives via Root's own DirectModules ([core]) and inherits down through the seed.
        IReadOnlySet<Identification> parentChainComposedSet = tempMetadata.ParentId.IsEmpty()
            ? new HashSet<Identification>()
            : ComposeState(tempMetadata.ParentId, tempStates, moduleCompositions, composedStates).ComposedSet;

        var declaration = new StateComposition(tempMetadata.Id, tempMetadata.ParentId, tempMetadata.ModuleIds);
        var composed = StateComposer.Compose(declaration, parentChainComposedSet, moduleCompositions);
        composedStates[stateId] = composed;
        return composed;
    }

    private void ValidateStateHierarchy(Identification stateId, StateMetadata metadata,
        Dictionary<Identification, StateMetadata> allStates)
    {
        var visited = new HashSet<Identification>();
        var current = metadata;

        // Walk up the parent chain
        while (!current.ParentId.IsEmpty())
        {
            if (!visited.Add(current.Id))
            {
                throw new InvalidOperationException(
                    $"Cyclic state hierarchy detected: {stateId}");
            }

            if (!allStates.TryGetValue(current.ParentId, out var parent))
            {
                throw new InvalidOperationException(
                    $"State {stateId} has invalid parent {current.ParentId}");
            }

            current = parent;
        }

        // At this point, current is the state with ParentId == Empty
        // This must be the Root state
        var rootStateId = StateID.Sparkitect.Root;
        if (!current.Id.Equals(rootStateId))
        {
            throw new InvalidOperationException(
                $"State {stateId} hierarchy does not terminate at Root state. " +
                $"Found {current.Id} with Empty parent, but only Root state may have Empty parent.");
        }
    }

    private List<Identification> GetAllInheritedModules(Identification stateId)
    {
        var modules = new List<Identification>();
        var current = stateId;

        while (!current.IsEmpty())
        {
            if (!_registeredStates.TryGetValue(current, out var state))
                break;

            modules.AddRange(state.ModuleIds);
            current = state.ParentId;
        }

        return modules;
    }

private ICoreContainer BuildContainerForState(Identification stateId, ICoreContainer parentContainer, IEnumerable<string> additionalMods)
    {
        if (!_registeredStates.TryGetValue(stateId, out var stateMetadata))
        {
            throw new InvalidOperationException($"State {stateId} is not registered");
        }

        // Get module types for delta modules
        var moduleTypes = new HashSet<Type>();
        foreach (var moduleId in stateMetadata.ModuleIds)
        {
            if (!_registeredModules.TryGetValue(moduleId, out var moduleMeta))
            {
                throw new InvalidOperationException($"Module {moduleId} referenced by state {stateId} is not registered");
            }
            moduleTypes.Add(moduleMeta.ModuleType);
        }

        // Register services for the state's delta modules only. Inherited modules (including the
        // ambient root seed) fall out of the delta via the generic parent-chain rule, so no module
        // needs a type-identity special-case here.
        return DIService.BuildConfiguredContainer<IStateModuleServiceConfigurator>(
            parentContainer,
            LoadedMods.Concat(additionalMods),
            typeof(StateModuleServiceConfiguratorEntrypointAttribute),
            (configurator, builder, loadedMods) =>
            {
                if (!moduleTypes.Contains(configurator.ModuleType)) return;

                configurator.Configure(builder, loadedMods);
                Log.Debug("Registered services for module {ModuleType} in state {StateId}",
                    configurator.ModuleType.Name, stateId);
            });
    }

    [DebuggerStepThrough]
    private void ExecuteMethods(IReadOnlyList<IStatelessFunction> methods)
    {
        foreach (var method in methods)
        {
            method.Execute();
        }
    }

    // Enter/exit method passes carry a first-class direction signal the registry manager reads to auto-detect
    // populate vs teardown. Per-frame execution keeps the default None.
    private void ExecuteEnterMethods(IReadOnlyList<IStatelessFunction> methods)
    {
        _transitionDirection = GsmTransitionDirection.Enter;
        try { ExecuteMethods(methods); }
        finally { _transitionDirection = GsmTransitionDirection.None; }
    }

    private void ExecuteExitMethods(IReadOnlyList<IStatelessFunction> methods)
    {
        _transitionDirection = GsmTransitionDirection.Exit;
        try { ExecuteMethods(methods); }
        finally { _transitionDirection = GsmTransitionDirection.None; }
    }

    // A module's registries are added when it becomes active (its delta on state enter) and removed when the
    // frame is popped; instance lifecycle is bookkeeping only, driven by the IRegistry<TModule> owning-module link.
    private void EnterFrameRegistries(ActiveStateFrame frame)
    {
        foreach (var moduleId in frame.AddedModuleIds)
            RegistryLifecycle.AddModuleRegistries(moduleId, frame.Container);
    }

    private void LeaveFrameRegistries(ActiveStateFrame frame)
    {
        foreach (var moduleId in frame.AddedModuleIds)
            RegistryLifecycle.RemoveModuleRegistries(moduleId);
    }

    private ActiveStateFrame CreateStateFrame(
        Identification stateId,
        ICoreContainer parentContainer,
        IReadOnlyList<string> additionalMods)
    {
        var container = BuildContainerForState(stateId, parentContainer, additionalMods);

        if (!_registeredStates.TryGetValue(stateId, out var stateMetadata))
        {
            throw new InvalidOperationException($"State {stateId} is not registered");
        }

        var allMods = LoadedMods.Concat(additionalMods).ToArray();
        var provider = new FacadeResolutionProvider();
        var wrapperTypes = FunctionManager.GetRegisteredWrapperTypes();
        var scope = DIService.BuildScope(container, provider, allMods, wrapperTypes);

        // Build state stack: existing frames + new state
        var stateStack = _stateStack
            .Reverse()
            .Select(f => ((IReadOnlyList<Identification>)_registeredStates[f.StateId].ModuleIds.ToList(), f.StateId))
            .Append(((IReadOnlyList<Identification>)stateMetadata.ModuleIds.ToList(), stateId))
            .ToList();

        // Modules declared by the never-framed Root anchor are ambiently loaded for frame gates: Root is
        // registered but never pushed as a frame, so its modules appear in no stack entry. Root's composed
        // set is the ambient universe the frame gates treat as always-present (agrees with the composer).
        var ambientModules = _registeredStates.TryGetValue(StateID.Sparkitect.Root, out var rootMeta)
            ? (IReadOnlyList<Identification>)rootMeta.ComposedSet.ToList()
            : (IReadOnlyList<Identification>)[];

        var transitionEnterCtx = new TransitionContext
        {
            StateStack = stateStack,
            IsEnterTransition = true,
            AmbientModules = ambientModules
        };
        var transitionExitCtx = transitionEnterCtx with { IsEnterTransition = false };

        var perFrameCtx = new PerFrameContext { StateStack = stateStack, AmbientModules = ambientModules };

        // Collect ALL scheduling metadata
        var metadata = new Dictionary<Identification, IScheduling>();
        using (var schedulingContainer = DIService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<IScheduling>>(allMods))
        {
            schedulingContainer.ProcessMany(ep => ep.CollectMetadata(metadata));
        }

        // Build PerFrame graph
        var perFrameBuilder = FunctionManager.CreateGraphBuilder();
        foreach (var (id, scheduling) in metadata)
        {
            if (scheduling is PerFrameScheduling pfs)
                pfs.BuildGraph(perFrameBuilder, perFrameCtx, id);
        }
        var perFrameMethods = FunctionManager.InstantiateWrappers(perFrameBuilder.Resolve(), scope);

        // Build Transition enter graph
        var transitionEnterBuilder = FunctionManager.CreateGraphBuilder();
        foreach (var (id, scheduling) in metadata)
        {
            if (scheduling is OnCreateScheduling ocs)
                ocs.BuildGraph(transitionEnterBuilder, transitionEnterCtx, id);
            else if (scheduling is OnFrameEnterScheduling ofes)
                ofes.BuildGraph(transitionEnterBuilder, transitionEnterCtx, id);
        }
        var transitionEnterMethods = FunctionManager.InstantiateWrappers(transitionEnterBuilder.Resolve(), scope);

        // Build Transition exit graph
        var transitionExitBuilder = FunctionManager.CreateGraphBuilder();
        foreach (var (id, scheduling) in metadata)
        {
            if (scheduling is OnDestroyScheduling ods)
                ods.BuildGraph(transitionExitBuilder, transitionExitCtx, id);
            else if (scheduling is OnFrameExitScheduling ofxs)
                ofxs.BuildGraph(transitionExitBuilder, transitionExitCtx, id);
        }
        var transitionExitMethods = FunctionManager.InstantiateWrappers(transitionExitBuilder.Resolve(), scope);

        return new ActiveStateFrame(
            stateId,
            container,
            additionalMods,
            stateMetadata.ModuleIds,
            transitionEnterMethods,
            transitionExitMethods,
            perFrameMethods);
    }

    private void PushState(ActiveStateFrame frame)
    {
        _stateStack.Push(frame);
    }

    private ActiveStateFrame PopState()
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop from empty state stack");
        }

        var frame = _stateStack.Pop();

        // Auto-remove the registries owned by this frame's modules (bookkeeping only).
        LeaveFrameRegistries(frame);

        // Unload mods if this frame added any
        if (frame.AddedMods.Count > 0)
        {
            var unloadedMods = ModManager.UnloadLastModGroup();
            Log.Debug("Unloaded {ModCount} mods when popping state {StateId}", unloadedMods.Count, frame.StateId);
        }

        // Dispose container
        frame.Container.Dispose();

        return frame;
    }

    private void TransitionToParent()
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot transition to parent from empty stack");
        }

        var childFrame = _stateStack.Peek();

        // Execute child exit sequence
        ExecuteExitMethods(childFrame.TransitionExitMethods);

        // Pop child
        PopState();

        // Parent is now leaf - execute parent enter transition (its registries stay added from first entry)
        if (_stateStack.Count > 0)
        {
            var parentFrame = _stateStack.Peek();
            ExecuteEnterMethods(parentFrame.TransitionEnterMethods);
            Log.Information("Transitioned to parent: {StateId}", parentFrame.StateId);
        }
    }

    private void TransitionToChild(
        Identification childStateId,
        ICoreContainer parentContainer)
    {
        // Parent is currently leaf - execute parent exit transition
        if (_stateStack.Count > 0)
        {
            var parentFrame = _stateStack.Peek();
            ExecuteExitMethods(parentFrame.TransitionExitMethods);
        }

        // Create and push child
        var childFrame = CreateStateFrame(childStateId, parentContainer, []);
        PushState(childFrame);
        EnterFrameRegistries(childFrame);

        // Execute child entry sequence
        ExecuteEnterMethods(childFrame.TransitionEnterMethods);

        Log.Information("Transitioned to child: {StateId}", childStateId);
    }

    private void ExecuteTransition()
    {
        if (!_pendingTransitionTarget.HasValue)
            return;

        var targetStateId = _pendingTransitionTarget.Value;
        _pendingTransitionTarget = null;

        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot transition from empty stack");
        }

        var currentStateId = _stateStack.Peek().StateId;

        if (currentStateId.Equals(targetStateId))
        {
            Log.Debug("Already in target state {StateId}, ignoring transition", targetStateId);
            return;
        }

        _isTransitioning = true;

        try
        {
            if (!_registeredStates.TryGetValue(currentStateId, out var currentState))
            {
                throw new InvalidOperationException($"Current state {currentStateId} is not registered");
            }

            if (!_registeredStates.TryGetValue(targetStateId, out var targetState))
            {
                throw new InvalidOperationException($"Target state {targetStateId} is not registered");
            }

            // Determine if transition is to parent or child
            bool isParent = currentState.ParentId.Equals(targetStateId);

            if (isParent)
            {
                Log.Information("Transitioning from {Current} to parent {Target}", currentStateId, targetStateId);
                TransitionToParent();
            }
            else
            {
                // Must be child
                Log.Information("Transitioning from {Current} to child {Target}", currentStateId, targetStateId);

                var parentContainer = _stateStack.Peek().Container;
                TransitionToChild(targetStateId, parentContainer);
            }

            Log.Information("Transition complete: now in state {StateId}", _stateStack.Peek().StateId);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void StartMainLoop()
    {
        Log.Information("Starting main loop");

        while (!_shutdownRequested)
        {
            // Check and execute pending transition before frame
            if (_pendingTransitionTarget.HasValue && !_isTransitioning)
            {
                ExecuteTransition();
            }

            // Execute PerFrame methods from current leaf state
            if (_stateStack.Count > 0)
            {
                var currentFrame = _stateStack.Peek();
                ExecuteMethods(currentFrame.PerFrameMethods);
            }

            // Check and execute pending transition after frame
            if (_pendingTransitionTarget.HasValue && !_isTransitioning)
            {
                ExecuteTransition();
            }
        }

        Log.Information("Main loop exited, performing shutdown cleanup");

        // Unwind all nested states back to root
        while (_stateStack.Count > 1)
        {
            TransitionToParent();
        }

        // Exit and pop root state
        if (_stateStack.Count > 0)
        {
            var rootFrame = _stateStack.Peek();
            ExecuteExitMethods(rootFrame.TransitionExitMethods);
            PopState();
        }

        Log.Information("Shutdown complete");
    }

    /// <summary>
    /// Selects which root mods to load at startup based on configuration.
    /// </summary>
    /// <returns>Array of mod file identifiers to load as roots.</returns>
    /// <remarks>
    /// <para>Selection priority:</para>
    /// <list type="number">
    ///   <item>If roots.json config exists: load only specified mods</item>
    ///   <item>If no config but mods with IsRootMod=true exist: load those mods</item>
    /// </list>
    /// <para>Throws <see cref="InvalidOperationException"/> if neither source provides root mods.</para>
    /// </remarks>
    private ModFileIdentifier[] SelectRootMods()
    {
        var configPath = DefaultRootModConfigPath;
        var config = RootModConfiguration.LoadConfig(configPath);

        if (config != null)
        {
            // Config exists: load only specified mods
            return SelectRootModsFromConfig(config);
        }

        // No config: check for mods marked as IsRootMod
        var rootMods = ModManager.DiscoveredArchives
            .Where(m => m.IsRootMod)
            .Select(m => new ModFileIdentifier(m.Id, m.Version))
            .ToArray();

        if (rootMods.Length > 0)
        {
            Log.Information("No root mod config found. Loading {Count} mods with IsRootMod=true", rootMods.Length);
            return rootMods;
        }

        throw new InvalidOperationException(
            "No root mod configuration found. Create a roots.json file or set IsRootMod=true in a mod manifest.");
    }

    /// <summary>
    /// Selects root mods from configuration, validating each entry.
    /// </summary>
    /// <param name="config">The loaded root mod configuration.</param>
    /// <returns>Array of mod file identifiers to load.</returns>
    /// <exception cref="InvalidOperationException">Thrown when config has validation errors.</exception>
    private ModFileIdentifier[] SelectRootModsFromConfig(RootModConfig config)
    {
        var errors = new List<ValidationError>();
        var selectedIdentifiers = new List<ModFileIdentifier>();

        foreach (var entry in config.RootMods)
        {
            // Find matching manifest
            ModManifest? selectedManifest;
            if (entry.Version != null)
            {
                // Exact version match requested
                selectedManifest = ModManager.DiscoveredArchives
                    .FirstOrDefault(m => m.Id == entry.Id && m.Version == entry.Version);
            }
            else
            {
                // No version specified: pick newest using Semver comparison (stable > prerelease)
                selectedManifest = ModManager.DiscoveredArchives
                    .Where(m => m.Id == entry.Id)
                    .OrderByDescending(m => m.Version)
                    .FirstOrDefault();
            }

            if (selectedManifest == null)
            {
                errors.Add(new ValidationError.NotFound(entry.Id));
                continue;
            }

            // Validate IsRootMod flag
            if (!selectedManifest.IsRootMod)
            {
                errors.Add(new ValidationError.NotRootMod(entry.Id));
                continue;
            }

            selectedIdentifiers.Add(new ModFileIdentifier(selectedManifest.Id, selectedManifest.Version));
        }

        if (errors.Count > 0)
        {
            var errorMessages = string.Join(Environment.NewLine, errors.Select(e => $"  - {e.Message}"));
            throw new InvalidOperationException($"Root mod configuration errors:{Environment.NewLine}{errorMessages}");
        }

        Log.Information("Root mod config loaded. Loading {Count} root mods", selectedIdentifiers.Count);
        return selectedIdentifiers.ToArray();
    }
}
