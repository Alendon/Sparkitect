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

    private readonly List<Func<StateMetadata>> _pendingStates = new();
    private readonly List<Func<ModuleMetadata>> _pendingModules = new();

    private Identification? _pendingTransitionTarget;
    private bool _isTransitioning;
    private bool _shutdownRequested;

    public required IModManager ModManager { get; init; }

    public ICoreContainer CurrentCoreContainer => _stateStack.Count > 0 ? _stateStack.Peek().Container : RootContainer;
    public ICoreContainer RootContainer { get; set; } = null!;

    /// <summary>
    /// Enter the Root Game State. To be called by the Engine Bootstrapper
    /// </summary>
    public void EnterRootState()
    {
        var rootStateId = StateID.Sparkitect.Root;

        if (!_registeredStates.ContainsKey(rootStateId))
        {
            throw new InvalidOperationException($"Root state {rootStateId} is not registered");
        }

        Log.Information("Entering root state: {StateId}", rootStateId);

        // Load infrastructure for state method instantiation
        var facadeMap = LoadFacadeMap();
        var methodAssociations = LoadMethodAssociations();
        var methodOrdering = LoadMethodOrdering();

        // Create root state frame using RootContainer
        var rootFrame = CreateStateFrame(rootStateId, RootContainer, facadeMap, methodAssociations, methodOrdering);
        PushState(rootFrame);

        // Execute entry sequence
        ExecuteMethods(rootFrame.OnCreateMethods);
        ExecuteMethods(rootFrame.OnFrameEnterMethods);

        Log.Information("Root state active, starting main loop");
        StartMainLoop();
    }

    public void Request(Identification stateId, object? payload = null)
    {
        if (_pendingTransitionTarget.HasValue)
        {
            Log.Warning("Overwriting pending transition to {OldTarget} with new target {NewTarget}",
                _pendingTransitionTarget.Value, stateId);
        }

        _pendingTransitionTarget = stateId;
        Log.Debug("Queued state transition to {StateId}", stateId);
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

    private IReadOnlyDictionary<Type, Type> LoadFacadeMap()
    {
        var facadeHolder = new FacadeHolder();

        using var facadeContainer = ModManager.CreateEntrypointContainer<IFacadeConfigurator<StateFacadeAttribute>>(new All());
        facadeContainer.ProcessMany(x => x.ConfigureFacades(facadeHolder));

        return facadeHolder.GetFacadeMapping();
    }

    private IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type> LoadMethodAssociations()
    {
        var builder = new StateMethodAssociationBuilder();

        using var associationContainer = ModManager.CreateEntrypointContainer<StateMethodAssociation>(new All());
        associationContainer.ProcessMany(x => x.Configure(builder));

        return builder.Build();
    }

    private HashSet<OrderingEntry> LoadMethodOrdering()
    {
        var ordering = new HashSet<OrderingEntry>();

        using var orderingContainer = ModManager.CreateEntrypointContainer<StateMethodOrdering>(new All());
        orderingContainer.ProcessMany(x => x.ConfigureOrdering(ordering));

        return ordering;
    }

    private ICoreContainer BuildContainerForState(Identification stateId, ICoreContainer parentContainer)
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
        using var configuratorContainer = ModManager.CreateEntrypointContainer<IStateModuleServiceConfigurator>(new All());
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
        IReadOnlyDictionary<Type, Type> facadeMap,
        IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type> methodAssociations,
        IReadOnlySet<OrderingEntry> methodOrdering)
    {
        var container = BuildContainerForState(stateId, parentContainer);

        if (!_registeredStates.TryGetValue(stateId, out var stateMetadata))
        {
            throw new InvalidOperationException($"State {stateId} is not registered");
        }

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

    private (List<Identification> exitPath, List<Identification> enterPath) CalculateTransitionPath(
        Identification currentStateId,
        Identification targetStateId)
    {
        // Build path from current to root
        var currentPath = new List<Identification>();
        var current = currentStateId;
        while (!current.Equals(Identification.Empty))
        {
            currentPath.Add(current);
            if (!_registeredStates.TryGetValue(current, out var state))
                break;
            current = state.ParentId;
        }

        // Build path from target to root
        var targetPath = new List<Identification>();
        var target = targetStateId;
        while (!target.Equals(Identification.Empty))
        {
            targetPath.Add(target);
            if (!_registeredStates.TryGetValue(target, out var state))
                break;
            target = state.ParentId;
        }

        // Find LCA (Lowest Common Ancestor)
        Identification lca = Identification.Empty;
        for (int i = currentPath.Count - 1, j = targetPath.Count - 1; i >= 0 && j >= 0; i--, j--)
        {
            if (currentPath[i].Equals(targetPath[j]))
            {
                lca = currentPath[i];
            }
            else
            {
                break;
            }
        }

        if (lca.Equals(Identification.Empty))
        {
            throw new InvalidOperationException($"No common ancestor found between {currentStateId} and {targetStateId}");
        }

        // Exit path: current -> LCA (excluding LCA)
        var exitPath = new List<Identification>();
        foreach (var id in currentPath)
        {
            if (id.Equals(lca))
                break;
            exitPath.Add(id);
        }

        // Enter path: LCA -> target (excluding LCA)
        var enterPath = new List<Identification>();
        foreach (var id in targetPath)
        {
            if (id.Equals(lca))
                break;
            enterPath.Add(id);
        }
        enterPath.Reverse(); // Need to go from LCA down to target

        return (exitPath, enterPath);
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
        ICoreContainer parentContainer,
        IReadOnlyDictionary<Type, Type> facadeMap,
        IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type> methodAssociations,
        IReadOnlySet<OrderingEntry> methodOrdering)
    {
        // Parent is currently leaf - execute parent OnFrameExit
        if (_stateStack.Count > 0)
        {
            var parentFrame = _stateStack.Peek();
            ExecuteMethods(parentFrame.OnFrameExitMethods);
        }

        // Create and push child
        var childFrame = CreateStateFrame(childStateId, parentContainer, facadeMap, methodAssociations, methodOrdering);
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
            // Load infrastructure maps fresh for this transition
            var facadeMap = LoadFacadeMap();
            var methodAssociations = LoadMethodAssociations();
            var methodOrdering = LoadMethodOrdering();

            var (exitPath, enterPath) = CalculateTransitionPath(currentStateId, targetStateId);

            Log.Information("Transitioning from {Current} to {Target} (exit: {ExitCount}, enter: {EnterCount})",
                currentStateId, targetStateId, exitPath.Count, enterPath.Count);

            // Execute atomic transitions up to LCA
            foreach (var _ in exitPath)
            {
                TransitionToParent();
            }

            // Execute atomic transitions down to target
            foreach (var childStateId in enterPath)
            {
                var parentContainer = _stateStack.Count > 0 ? _stateStack.Peek().Container : RootContainer;
                TransitionToChild(childStateId, parentContainer, facadeMap, methodAssociations, methodOrdering);
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
