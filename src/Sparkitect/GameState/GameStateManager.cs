using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using OneOf.Types;
using QuikGraph;
using QuikGraph.Algorithms;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

/// <summary>
/// Manages game state transitions, module lifecycle, and main loop execution
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade, IGameStateManagerStateFacade
{
    private readonly Dictionary<Identification, StateMetadata> _registeredStates = new();
    private readonly Dictionary<Identification, ModuleMetadata> _registeredModules = new();
    private readonly Stack<ActiveStateFrame> _stateStack = new();
    private readonly HashSet<Identification> _addedModules = new();

    public IEnumerable<string> LoadedMods => _stateStack.SelectMany(x => x.AddedMods);

    private readonly List<Func<StateMetadata>> _pendingStates = new();
    private readonly List<Func<ModuleMetadata>> _pendingModules = new();

    private Identification? _pendingTransitionTarget;
    private bool _isTransitioning;
    private bool _shutdownRequested;

    public required IModManager ModManager { get; init; }
    public required IRegistryManager RegistryManager { get; init; }
    public required IModDIService ModDIService { get; init; }

    public ICoreContainer CurrentCoreContainer => _stateStack.Count > 0 ? _stateStack.Peek().Container : RootContainer;
    public ICoreContainer RootContainer { get; set; } = null!;

    /// <summary>
    /// Enter the Root Game State. To be called by the Engine Bootstrapper
    /// </summary>
    public void EnterRootState()
    {
        Log.Information("Entering root state");

        // Discover and load root mods
        var rootMods = ModManager.DiscoveredArchives
            .Select(a => a.Id)
            .ToArray();

        Log.Information("Loading {ModCount} root mods", rootMods.Length);
        ModManager.LoadMods(rootMods);
        
        RegistryManager.AddRegistry<ModuleRegistry>();
        RegistryManager.AddRegistry<StateRegistry>();

        // Process registries for root mods
        RegistryManager.ProcessRegistry<ModuleRegistry>(rootMods);
        RegistryManager.ProcessRegistry<StateRegistry>(rootMods);

        // Finalize registrations
        FinalizeRegistrations();

        // Query entry state selector to determine initial active state
        // Note: Root state is registered but never framed - it's a semantic anchor
        using var entrySelectorContainer = ModDIService.CreateEntrypointContainer<IEntryStateSelector>(rootMods);
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
        var entryFrame = CreateStateFrame(entryStateId, RootContainer, rootMods);

        PushState(entryFrame);

        // Execute entry sequence
        ExecuteMethods(entryFrame.OnCreateMethods);
        ExecuteMethods(entryFrame.OnFrameEnterMethods);

        Log.Information("Entry state active, starting main loop");
        StartMainLoop();
    }

    public void Request(Identification stateId, object? payload = null)
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
            Log.Warning("Overwriting pending transition to {OldTarget} with new target {NewTarget}",
                _pendingTransitionTarget.Value, stateId);
        }

        _pendingTransitionTarget = stateId;
        Log.Debug("Queued state transition to {StateId}", stateId);
    }

    public void RequestWithModChange(Func<Identification> stateIdFunc, IReadOnlyList<string> additionalMods, object? payload = null)
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot transition from empty stack");
        }

        var currentStateId = _stateStack.Peek().StateId;

        Log.Information("Loading {ModCount} additional mods for state transition", additionalMods.Count);

        // Load the mods
        ModManager.LoadMods(additionalMods.ToArray());

        // Process registries for the newly loaded mods
        RegistryManager.ProcessRegistry<ModuleRegistry>(additionalMods);
        RegistryManager.ProcessRegistry<StateRegistry>(additionalMods);

        // Finalize registrations (validates state/module hierarchy)
        FinalizeRegistrations();

        // Now compute the target state ID
        var targetStateId = stateIdFunc();

        // Prevent transitioning to Root state (it's never framed)
        var rootStateId = StateID.Sparkitect.Root;
        if (targetStateId.Equals(rootStateId))
        {
            throw new InvalidOperationException(
                "Cannot transition to Root state. Root is a semantic anchor and cannot be an active state.");
        }

        // Validate target is child of current
        if (!_registeredStates.TryGetValue(targetStateId, out var targetState))
        {
            throw new InvalidOperationException($"Target state {targetStateId} is not registered");
        }

        if (!targetState.ParentId.Equals(currentStateId))
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
            ExecuteMethods(parentFrame.OnFrameExitMethods);

            // Create child frame
            var parentContainer = _stateStack.Peek().Container;
            var childFrame = CreateStateFrame(targetStateId, parentContainer, additionalMods);

            // Reconstruct frame with AddedMods
            childFrame = childFrame with { AddedMods = additionalMods };

            PushState(childFrame);

            // Execute child entry sequence
            ExecuteMethods(childFrame.OnCreateMethods);
            ExecuteMethods(childFrame.OnFrameEnterMethods);

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

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        _pendingModules.Add(() =>
        {
            var identification = TStateModule.Identification;
            var requiredModules = TStateModule.RequiredModules.ToArray();
            return new ModuleMetadata(identification, requiredModules, typeof(TStateModule));
        });
    }

    public void RemoveStateModule(Identification id)
    {
        if (_registeredModules.Remove(id))
        {
            Log.Debug("Removed module: {ModuleId}", id);
        }
    }

    public void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        _pendingStates.Add(() =>
        {
            var identification = TStateDescriptor.Identification;
            var parentId = TStateDescriptor.ParentId;
            var modules = TStateDescriptor.Modules;
            return new StateMetadata(identification, parentId, modules, typeof(TStateDescriptor));
        });
    }

    public void RemoveStateDescriptor(Identification id)
    {
        if (_registeredStates.Remove(id))
        {
            Log.Debug("Removed state: {StateId}", id);
        }
    }

    public void FinalizeRegistrations()
    {
        // Register modules
        foreach (var moduleFactory in _pendingModules)
        {
            var metadata = moduleFactory();
            _registeredModules[metadata.Id] = metadata;
            Log.Debug("Finalized module registration: {ModuleId}", metadata.Id);
        }
        _pendingModules.Clear();

        // Register states (initial pass)
        var tempStates = new Dictionary<Identification, StateMetadata>();
        foreach (var stateFactory in _pendingStates)
        {
            var metadata = stateFactory();
            tempStates[metadata.Id] = metadata;
        }
        _pendingStates.Clear();

        // Process each state: validate hierarchy, compute delta modules, validate dependencies
        foreach (var (stateId, tempMetadata) in tempStates)
        {
            // Validate path to root
            ValidateStateHierarchy(stateId, tempMetadata, tempStates);

            // Get accumulated modules from parent chain
            var parentModules = GetParentModules(tempMetadata.ParentId, tempStates);

            // Compute delta (new modules this state adds)
            var deltaModules = new List<Identification>();
            foreach (var moduleId in tempMetadata.ModuleIds)
            {
                if (parentModules.Contains(moduleId))
                {
                    Log.Warning("State {StateId} declares module {ModuleId} which is already provided by parent",
                        stateId, moduleId);
                }
                else
                {
                    deltaModules.Add(moduleId);
                }
            }

            // Validate module dependencies
            var allModules = new HashSet<Identification>(parentModules);
            allModules.UnionWith(deltaModules);
            ValidateModuleDependencies(stateId, deltaModules, allModules);

            // Create final metadata with delta modules
            var finalMetadata = new StateMetadata(
                tempMetadata.Id,
                tempMetadata.ParentId,
                deltaModules,
                tempMetadata.DescriptorType);

            _registeredStates[stateId] = finalMetadata;
            Log.Debug("Finalized state registration: {StateId} (parent: {ParentId}, new modules: {ModuleCount})",
                stateId, tempMetadata.ParentId, deltaModules.Count);
        }

        Log.Information("Finalized {ModuleCount} modules and {StateCount} states",
            _registeredModules.Count, _registeredStates.Count);
    }

    private void ValidateStateHierarchy(Identification stateId, StateMetadata metadata,
        Dictionary<Identification, StateMetadata> allStates)
    {
        var visited = new HashSet<Identification>();
        var current = metadata;

        // Walk up the parent chain
        while (!current.ParentId.Equals(Identification.Empty))
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

    private HashSet<Identification> GetParentModules(Identification parentId,
        Dictionary<Identification, StateMetadata> allStates)
    {
        var modules = new HashSet<Identification>();
        var current = parentId;

        while (!current.Equals(Identification.Empty))
        {
            if (!allStates.TryGetValue(current, out var state))
                break;

            foreach (var moduleId in state.ModuleIds)
            {
                modules.Add(moduleId);
            }

            current = state.ParentId;
        }

        return modules;
    }

    private void ValidateModuleDependencies(Identification stateId,
        List<Identification> newModules, HashSet<Identification> allModules)
    {
        foreach (var moduleId in newModules)
        {
            if (!_registeredModules.TryGetValue(moduleId, out var moduleMeta))
            {
                throw new InvalidOperationException(
                    $"State {stateId} references unregistered module {moduleId}");
            }

            foreach (var requiredId in moduleMeta.RequiredModules)
            {
                if (!allModules.Contains(requiredId))
                {
                    throw new InvalidOperationException(
                        $"Module {moduleId} in state {stateId} requires {requiredId} which is not available");
                }
            }
        }
    }

    private List<Identification> GetAllInheritedModules(Identification stateId)
    {
        var modules = new List<Identification>();
        var current = stateId;

        while (!current.Equals(Identification.Empty))
        {
            if (!_registeredStates.TryGetValue(current, out var state))
                break;

            modules.AddRange(state.ModuleIds);
            current = state.ParentId;
        }

        return modules;
    }

    private IReadOnlyDictionary<Type, Type> LoadFacadeMap(IEnumerable<string> additionalMods)
    {
        var facadeHolder = new FacadeHolder();

        using var facadeContainer = ModDIService.CreateEntrypointContainer<IFacadeConfigurator<StateFacadeAttribute>>(LoadedMods.Concat(additionalMods));
        facadeContainer.ProcessMany(x => x.ConfigureFacades(facadeHolder));

        return facadeHolder.GetFacadeMapping();
    }

    private IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type> LoadMethodAssociations(IEnumerable<string> additionalMods)
    {
        var builder = new StateMethodAssociationBuilder();

        using var associationContainer = ModDIService.CreateEntrypointContainer<StateMethodAssociation>(LoadedMods.Concat(additionalMods));
        associationContainer.ProcessMany(x => x.Configure(builder));

        return builder.Build();
    }

    private HashSet<OrderingEntry> LoadMethodOrdering(IEnumerable<string> additionalMods)
    {
        var ordering = new HashSet<OrderingEntry>();

        using var orderingContainer = ModDIService.CreateEntrypointContainer<StateMethodOrdering>(LoadedMods.Concat(additionalMods));
        orderingContainer.ProcessMany(x => x.ConfigureOrdering(ordering));

        return ordering;
    }

    private ICoreContainer BuildContainerForState(Identification stateId, ICoreContainer parentContainer, IEnumerable<string> additionalMods)
    {
        if (!_registeredStates.TryGetValue(stateId, out var stateMetadata))
        {
            throw new InvalidOperationException($"State {stateId} is not registered");
        }

        var builder = new CoreContainerBuilder(parentContainer);

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

        // Register services for new modules
        using var configuratorContainer = ModDIService.CreateEntrypointContainer<IStateModuleServiceConfigurator>(LoadedMods.Concat(additionalMods));
        configuratorContainer.ProcessMany(configurator =>
        {
            if (!moduleTypes.Contains(configurator.ModuleType)) return;

            configurator.ConfigureServices(builder);
            Log.Debug("Registered services for module {ModuleType} in state {StateId}",
                configurator.ModuleType.Name, stateId);
        });

        return builder.Build();
    }

    private Dictionary<string, IStateMethod> InstantiateStateMethods(
        Identification parentId,
        ICoreContainer container,
        IReadOnlyDictionary<Type, Type> facadeMap,
        IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type> methodAssociations,
        StateMethodSchedule schedule)
    {
        var methods = new Dictionary<string, IStateMethod>();

        foreach (var ((id, key, sch), wrapperType) in methodAssociations)
        {
            if (!id.Equals(parentId) || sch != schedule) continue;
            var wrapper = (IStateMethod)Activator.CreateInstance(wrapperType)!;
            wrapper.Initialize(container, facadeMap);
            methods[key] = wrapper;
        }

        return methods;
    }

    private IReadOnlyList<IStateMethod> SortMethods(
        Identification parentId,
        Dictionary<string, IStateMethod> methods,
        IReadOnlySet<OrderingEntry> ordering)
    {
        if (methods.Count == 0)
            return Array.Empty<IStateMethod>();

        // Build dependency graph
        var graph = new AdjacencyGraph<string, Edge<string>>();

        foreach (var key in methods.Keys)
        {
            graph.AddVertex(key);
        }

        // Add edges from ordering constraints
        foreach (var entry in ordering)
        {
            // entry.Before should execute before entry.After
            // So add edge: After -> Before (reversed because topological sort)
            if (entry.Before.Parent.Equals(parentId) && entry.After.Parent.Equals(parentId))
            {
                if (methods.ContainsKey(entry.Before.Method) && methods.ContainsKey(entry.After.Method))
                {
                    graph.AddEdge(new Edge<string>(entry.After.Method, entry.Before.Method));
                }
            }
        }

        // Topological sort
        var sortedKeys = graph.TopologicalSort();
        var result = new List<IStateMethod>();

        foreach (var key in sortedKeys)
        {
            result.Add(methods[key]);
        }

        return result;
    }

    private void ExecuteMethods(IReadOnlyList<IStateMethod> methods)
    {
        foreach (var method in methods)
        {
            method.Execute();
        }
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

        var facadeMap = LoadFacadeMap(additionalMods);
        var methodAssociations = LoadMethodAssociations(additionalMods);
        var methodOrdering = LoadMethodOrdering(additionalMods);

        var deltaModules = stateMetadata.ModuleIds;
        var allModules = GetAllInheritedModules(stateId);

        // Collect onCreate/onDestroy from state + delta modules only
        var onCreateDict = InstantiateStateMethods(stateId, container, facadeMap, methodAssociations, StateMethodSchedule.OnCreate);
        var onDestroyDict = InstantiateStateMethods(stateId, container, facadeMap, methodAssociations, StateMethodSchedule.OnDestroy);

        foreach (var moduleId in deltaModules)
        {
            var moduleCreate = InstantiateStateMethods(moduleId, container, facadeMap, methodAssociations, StateMethodSchedule.OnCreate);
            var moduleDestroy = InstantiateStateMethods(moduleId, container, facadeMap, methodAssociations, StateMethodSchedule.OnDestroy);

            foreach (var (key, method) in moduleCreate)
                onCreateDict[key] = method;

            foreach (var (key, method) in moduleDestroy)
                onDestroyDict[key] = method;

            // Track module as added
            if (!_addedModules.Add(moduleId))
            {
                Log.Warning("Module {ModuleId} added by state {StateId} was already added by parent",
                    moduleId, stateId);
            }
        }

        // Collect onFrameEnter/onFrameExit/PerFrame from state + all modules
        var perFrameDict = InstantiateStateMethods(stateId, container, facadeMap, methodAssociations, StateMethodSchedule.PerFrame);
        var onFrameEnterDict = InstantiateStateMethods(stateId, container, facadeMap, methodAssociations, StateMethodSchedule.OnFrameEnter);
        var onFrameExitDict = InstantiateStateMethods(stateId, container, facadeMap, methodAssociations, StateMethodSchedule.OnFrameExit);

        foreach (var moduleId in allModules)
        {
            var modulePerFrame = InstantiateStateMethods(moduleId, container, facadeMap, methodAssociations, StateMethodSchedule.PerFrame);
            var moduleFrameEnter = InstantiateStateMethods(moduleId, container, facadeMap, methodAssociations, StateMethodSchedule.OnFrameEnter);
            var moduleFrameExit = InstantiateStateMethods(moduleId, container, facadeMap, methodAssociations, StateMethodSchedule.OnFrameExit);

            foreach (var (key, method) in modulePerFrame)
                perFrameDict[key] = method;

            foreach (var (key, method) in moduleFrameEnter)
                onFrameEnterDict[key] = method;

            foreach (var (key, method) in moduleFrameExit)
                onFrameExitDict[key] = method;
        }

        // Sort all methods together
        var perFrameMethods = SortMethods(stateId, perFrameDict, methodOrdering);
        var onCreateMethods = SortMethods(stateId, onCreateDict, methodOrdering);
        var onDestroyMethods = SortMethods(stateId, onDestroyDict, methodOrdering);
        var onFrameEnterMethods = SortMethods(stateId, onFrameEnterDict, methodOrdering);
        var onFrameExitMethods = SortMethods(stateId, onFrameExitDict, methodOrdering);

        return new ActiveStateFrame(
            stateId,
            container,
            Array.Empty<string>(),
            perFrameMethods,
            onCreateMethods,
            onDestroyMethods,
            onFrameEnterMethods,
            onFrameExitMethods);
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

        // Unload mods if this frame added any
        if (frame.AddedMods.Count > 0)
        {
            var unloadedMods = ModManager.UnloadLastModGroup();
            Log.Debug("Unloaded {ModCount} mods when popping state {StateId}", unloadedMods.Count, frame.StateId);
        }

        // Dispose container
        frame.Container.Dispose();

        // Dispose methods if they implement IDisposable
        foreach (var method in frame.PerFrameMethods.Concat(frame.OnCreateMethods)
            .Concat(frame.OnDestroyMethods).Concat(frame.OnFrameEnterMethods)
            .Concat(frame.OnFrameExitMethods))
        {
            if (method is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

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
        ExecuteMethods(childFrame.OnFrameExitMethods);
        ExecuteMethods(childFrame.OnDestroyMethods);

        // Pop child
        PopState();

        // Parent is now leaf - execute parent OnFrameEnter
        if (_stateStack.Count > 0)
        {
            var parentFrame = _stateStack.Peek();
            ExecuteMethods(parentFrame.OnFrameEnterMethods);
            Log.Information("Transitioned to parent: {StateId}", parentFrame.StateId);
        }
    }

    private void TransitionToChild(
        Identification childStateId,
        ICoreContainer parentContainer)
    {
        // Parent is currently leaf - execute parent OnFrameExit
        if (_stateStack.Count > 0)
        {
            var parentFrame = _stateStack.Peek();
            ExecuteMethods(parentFrame.OnFrameExitMethods);
        }

        // Create and push child
        var childFrame = CreateStateFrame(childStateId, parentContainer, []);
        PushState(childFrame);

        // Execute child entry sequence
        ExecuteMethods(childFrame.OnCreateMethods);
        ExecuteMethods(childFrame.OnFrameEnterMethods);

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

        // Transition back to root state if not already there
        if (_stateStack.Count > 1)
        {
            var rootState = _stateStack.ToArray()[^1].StateId; // Bottom of stack
            Request(rootState);
            ExecuteTransition();
        }

        // Exit and pop root state
        if (_stateStack.Count > 0)
        {
            var rootFrame = _stateStack.Peek();
            ExecuteMethods(rootFrame.OnFrameExitMethods);
            ExecuteMethods(rootFrame.OnDestroyMethods);
            PopState();
        }

        Log.Information("Shutdown complete");
    }
}
