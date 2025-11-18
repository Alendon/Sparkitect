using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using OneOf;
using OneOf.Types;
using QuikGraph.Algorithms;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState;

/// <summary>
/// Manages game state transitions, module lifecycle, and main loop execution
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade
{
    // State/Module metadata storage
    private readonly Dictionary<Identification, StateMetadata> _states = new();
    private readonly Dictionary<Identification, ModuleMetadata> _modules = new();

    // Method registry (populated from entrypoints)
    private readonly Dictionary<(Identification ParentId, string MethodKey), Type> _methodWrappers = new();
    private readonly Dictionary<StateMethodSchedule, List<(Identification ParentId, string MethodKey)>> _methodsBySchedule = new();

    // Ordering constraints
    private readonly List<OrderingEntry> _orderingConstraints = new();

    // Active state tracking
    private readonly Stack<ActiveStateFrame> _stateStack = new();
    private ICoreContainer _currentContainer = null!;
    private bool _isRunning;
    private bool _shutdownRequested;
    private Identification? _pendingStateTransition;
    private object? _pendingPayload;

    public ICoreContainer CurrentCoreContainer => _currentContainer;

    internal required IModManager ModManager { get; init; }

    public void EnterRootState(ICoreContainer coreContainer)
    {
        Log.Information("Entering Root State");
        _currentContainer = coreContainer;

        // Process entrypoints to build method registry
        ProcessStateMethodAssociations();
        ProcessStateMethodOrdering();

        // Initialize root state (hardcoded for now)
        // Root state should be registered via registry system before this call
        var rootStateId = StateID.Sparkitect.Root;

        if (!_states.ContainsKey(rootStateId))
        {
            throw new InvalidOperationException("Root state not registered");
        }

        var rootStateMetadata = _states[rootStateId];

        // Build root container with root modules
        var rootContainer = BuildStateContainer(_currentContainer, rootStateMetadata.ModuleIds);

        // Execute OnModuleEnter for root modules
        var moduleEnterMethods = BuildOrderedMethodList(
            StateMethodSchedule.OnModuleEnter,
            rootStateMetadata.ModuleIds,
            Array.Empty<Identification>(),
            rootContainer);
        ExecuteOrderedMethods(moduleEnterMethods);

        // Execute OnStateEnter for root state
        var stateEnterMethods = BuildOrderedMethodList(
            StateMethodSchedule.OnStateEnter,
            rootStateMetadata.ModuleIds,
            new[] { rootStateId },
            rootContainer);
        ExecuteOrderedMethods(stateEnterMethods);

        // Build PerFrame method list
        var perFrameMethods = BuildOrderedMethodList(
            StateMethodSchedule.PerFrame,
            rootStateMetadata.ModuleIds,
            new[] { rootStateId },
            rootContainer);

        // Push root state to stack
        _stateStack.Push(new ActiveStateFrame(rootStateId, rootContainer, perFrameMethods));

        Log.Information("Root state initialized, starting game loop");

        // Run main loop (blocks until shutdown)
        RunGameLoop();
    }

    private void RunGameLoop()
    {
        _isRunning = true;

        while (_isRunning && _stateStack.Count > 0)
        {
            // Execute current state's PerFrame methods
            var currentFrame = _stateStack.Peek();
            ExecuteOrderedMethods(currentFrame.PerFrameMethods);

            // Process pending state transition if any
            if (_pendingStateTransition.HasValue)
            {
                var targetState = _pendingStateTransition.Value;
                var payload = _pendingPayload;

                _pendingStateTransition = null;
                _pendingPayload = null;

                ExecuteTransition(targetState, payload);
            }

            // Check shutdown flag
            if (_shutdownRequested)
            {
                _isRunning = false;
            }
        }

        Log.Information("Game loop ended");
    }

    public void Shutdown()
    {
        Log.Information("Shutdown requested");
        _shutdownRequested = true;
    }

    private void ProcessStateMethodAssociations()
    {
        Log.Debug("Processing state method associations");

        using var entrypointContainer = ModManager.CreateEntrypointContainer<StateMethodAssociation>(new OneOf.Types.All());

        var associations = entrypointContainer.ResolveMany();

        foreach (var association in associations)
        {
            var builder = new StateMethodAssociationBuilder();
            association.Configure(builder);

            var mappings = builder.Build();

            foreach (var (key, wrapperType) in mappings)
            {
                var (parentId, methodKey, scheduleType) = key;

                // Store wrapper type
                _methodWrappers[(parentId, methodKey)] = wrapperType;

                // Add to schedule index
                if (!_methodsBySchedule.TryGetValue(scheduleType, out var list))
                {
                    list = new List<(Identification, string)>();
                    _methodsBySchedule[scheduleType] = list;
                }

                list.Add((parentId, methodKey));
            }
        }

        Log.Debug("Registered {Count} state methods across {ScheduleCount} schedules",
            _methodWrappers.Count, _methodsBySchedule.Count);
    }

    private void ProcessStateMethodOrdering()
    {
        Log.Debug("Processing state method ordering");

        using var entrypointContainer = ModManager.CreateEntrypointContainer<StateMethodOrdering>(new OneOf.Types.All());

        var orderings = entrypointContainer.ResolveMany();

        foreach (var ordering in orderings)
        {
            var constraintSet = new HashSet<OrderingEntry>();
            ordering.ConfigureOrdering(constraintSet);

            _orderingConstraints.AddRange(constraintSet);
        }

        Log.Debug("Registered {Count} ordering constraints", _orderingConstraints.Count);
    }


    public void Request(Identification stateId, object? payload = null)
    {
        // Store pending transition to be applied after current loop iteration
        _pendingStateTransition = stateId;
        _pendingPayload = payload;

        Log.Debug("State transition requested to {StateId}", stateId);
    }

    private List<Identification> GetAncestorChain(Identification stateId)
    {
        var chain = new List<Identification>();

        var current = stateId;
        while (!current.Equals(Identification.Empty))
        {
            chain.Add(current);

            if (!_states.TryGetValue(current, out var metadata))
            {
                throw new InvalidOperationException($"State {current} not found in registry");
            }

            current = metadata.ParentId;
        }

        return chain;
    }

    private (List<Identification> ExitStates, Identification LCA, List<Identification> EnterStates) ComputeTransitionPath(
        Identification fromStateId, Identification toStateId)
    {
        // Build ancestor chains
        var fromChain = GetAncestorChain(fromStateId);
        var toChain = GetAncestorChain(toStateId);

        // Reverse to get root-to-leaf order
        fromChain.Reverse();
        toChain.Reverse();

        // Find LCA (lowest common ancestor)
        int lcaIndex = 0;
        while (lcaIndex < fromChain.Count &&
               lcaIndex < toChain.Count &&
               fromChain[lcaIndex].Equals(toChain[lcaIndex]))
        {
            lcaIndex++;
        }

        // LCA is the last matching state
        var lca = lcaIndex > 0 ? fromChain[lcaIndex - 1] : Identification.Empty;

        // Exit states: from current back to LCA (excluding LCA)
        var exitStates = new List<Identification>();
        for (int i = fromChain.Count - 1; i >= lcaIndex; i--)
        {
            exitStates.Add(fromChain[i]);
        }

        // Enter states: from LCA to target (excluding LCA, including target)
        var enterStates = new List<Identification>();
        for (int i = lcaIndex; i < toChain.Count; i++)
        {
            enterStates.Add(toChain[i]);
        }

        Log.Debug("Computed transition path: exit {ExitCount} states, LCA={LCA}, enter {EnterCount} states",
            exitStates.Count, lca, enterStates.Count);

        return (exitStates, lca, enterStates);
    }

    private IFacadedCoreContainer BuildStateContainer(ICoreContainer parentContainer, IEnumerable<Identification> moduleIds)
    {
        Log.Debug("Building state container for modules: {ModuleIds}", string.Join(", ", moduleIds));

        // Collect all used services from modules
        var usedServices = new HashSet<Type>();
        foreach (var moduleId in moduleIds)
        {
            if (_modules.TryGetValue(moduleId, out var module))
            {
                foreach (var serviceType in module.UsedServices)
                {
                    usedServices.Add(serviceType);
                }
            }
        }

        // Query facade configurators for all marker types
        var facadeHolder = new DI.FacadeHolder();

        // Query StateFacade configurators
        using (var stateFacadeContainer = ModManager.CreateEntrypointContainer<DI.IStateFacadeConfigurator>(new OneOf.Types.All()))
        {
            stateFacadeContainer.ProcessMany(x => x.ConfigureFacades(facadeHolder));
        }

        // Query RegistryFacade configurators (in case state services depend on registry facades)
        using (var registryFacadeContainer = ModManager.CreateEntrypointContainer<DI.IRegistryFacadeConfigurator>(new OneOf.Types.All()))
        {
            registryFacadeContainer.ProcessMany(x => x.ConfigureFacades(facadeHolder));
        }

        // Get all facade mappings and filter to only include facades for services used by active modules
        var allFacadeMappings = facadeHolder.GetFacadeMapping();
        var facadeMap = allFacadeMappings
            .Where(kvp => usedServices.Contains(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Build child container
        // Note: Service factories for state services should already be registered via StateServiceFactoryGenerator
        // and collected through configurator entrypoints. For now, we're just building the container
        // with the parent, assuming services are already available in parent or will be added by states.
        var containerBuilder = new CoreContainerBuilder(parentContainer);

        // TODO: Register state-specific service factories here if needed
        // This depends on how state services are registered - need to clarify registration mechanism

        var coreContainer = containerBuilder.Build();

        // Wrap in FacadedCoreContainer
        var facadedContainer = new FacadedCoreContainer(coreContainer, facadeMap);

        Log.Debug("Built state container with {ServiceCount} used services and {FacadeCount} facade mappings",
            usedServices.Count, facadeMap.Count);

        return facadedContainer;
    }

    private List<IStateMethod> BuildOrderedMethodList(
        StateMethodSchedule schedule,
        IEnumerable<Identification> activeModuleIds,
        IEnumerable<Identification> activeStateIds,
        IFacadedCoreContainer container)
    {
        // Get all methods for this schedule
        if (!_methodsBySchedule.TryGetValue(schedule, out var allMethods))
        {
            return new List<IStateMethod>();
        }

        // Filter to active parents (modules or states)
        var activeParents = new HashSet<Identification>(activeModuleIds.Concat(activeStateIds));
        var activeMethods = allMethods
            .Where(m => activeParents.Contains(m.Item1))
            .ToList();

        if (activeMethods.Count == 0)
        {
            return new List<IStateMethod>();
        }

        // Build ordering graph
        var graph = new QuikGraph.AdjacencyGraph<(Identification, string), QuikGraph.Edge<(Identification, string)>>();

        // Add vertices
        foreach (var method in activeMethods)
        {
            graph.AddVertex(method);
        }

        // Add edges from ordering constraints
        foreach (var constraint in _orderingConstraints)
        {
            var before = (constraint.Before.Parent, constraint.Before.Method);
            var after = (constraint.After.Parent, constraint.After.Method);

            // Only add edge if both vertices are in active set
            if (graph.ContainsVertex(before) && graph.ContainsVertex(after))
            {
                graph.AddEdge(new QuikGraph.Edge<(Identification, string)>(before, after));
            }
        }

        // Topological sort
        List<(Identification, string)> sortedMethods;
        try
        {
            sortedMethods = graph.TopologicalSort().ToList();
        }
        catch (QuikGraph.NonAcyclicGraphException)
        {
            Log.Error("Circular dependency detected in state method ordering for schedule {Schedule}", schedule);
            throw new InvalidOperationException($"Circular dependency in {schedule} method ordering");
        }

        // Reverse for exit schedules
        if (schedule == StateMethodSchedule.OnStateExit || schedule == StateMethodSchedule.OnModuleExit)
        {
            sortedMethods.Reverse();
        }

        // Instantiate wrappers and initialize
        var methods = new List<IStateMethod>();
        foreach (var (parentId, methodKey) in sortedMethods)
        {
            if (_methodWrappers.TryGetValue((parentId, methodKey), out var wrapperType))
            {
                if (Activator.CreateInstance(wrapperType) is IStateMethod method)
                {
                    method.Initialize(container);
                    methods.Add(method);
                }
            }
        }

        Log.Debug("Built ordered method list for {Schedule}: {Count} methods", schedule, methods.Count);

        return methods;
    }

    private void ExecuteOrderedMethods(IReadOnlyList<IStateMethod> methods)
    {
        foreach (var method in methods)
        {
            method.Execute();
        }
    }

    private void ExecuteTransition(Identification targetStateId, object? payload)
    {
        if (_stateStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot execute transition: no active state");
        }

        var currentStateId = _stateStack.Peek().StateId;

        Log.Information("Executing state transition from {FromState} to {ToState}", currentStateId, targetStateId);

        var (exitStates, lca, enterStates) = ComputeTransitionPath(currentStateId, targetStateId);

        // Exit Phase
        foreach (var stateId in exitStates)
        {
            var frame = _stateStack.Pop();
            var stateMetadata = _states[stateId];

            Log.Debug("Exiting state {StateId}", stateId);

            // Get active modules for this state
            var activeModules = stateMetadata.ModuleIds;

            // Determine departing modules (not in parent state)
            var departingModules = activeModules.ToList();
            if (!stateMetadata.ParentId.Equals(Identification.Empty) && _states.TryGetValue(stateMetadata.ParentId, out var parentMetadata))
            {
                departingModules = activeModules.Except(parentMetadata.ModuleIds).ToList();
            }

            // Execute OnStateExit methods
            var stateExitMethods = BuildOrderedMethodList(
                StateMethodSchedule.OnStateExit,
                activeModules,
                new[] { stateId },
                (IFacadedCoreContainer)frame.Container);
            ExecuteOrderedMethods(stateExitMethods);

            // Execute OnModuleExit methods
            if (departingModules.Count > 0)
            {
                var moduleExitMethods = BuildOrderedMethodList(
                    StateMethodSchedule.OnModuleExit,
                    departingModules,
                    Array.Empty<Identification>(),
                    (IFacadedCoreContainer)frame.Container);
                ExecuteOrderedMethods(moduleExitMethods);
            }

            // Dispose container
            frame.Container.Dispose();
        }

        // Enter Phase
        foreach (var stateId in enterStates)
        {
            var stateMetadata = _states[stateId];

            Log.Debug("Entering state {StateId}", stateId);

            // Get active modules for this state
            var activeModules = stateMetadata.ModuleIds;

            // Determine arriving modules (not in parent state)
            var arrivingModules = activeModules.ToList();
            if (!stateMetadata.ParentId.Equals(Identification.Empty) && _states.TryGetValue(stateMetadata.ParentId, out var parentMetadata))
            {
                arrivingModules = activeModules.Except(parentMetadata.ModuleIds).ToList();
            }

            // Get parent container (from stack top, or current container if stack empty)
            var parentContainer = _stateStack.Count > 0 ? _stateStack.Peek().Container : _currentContainer;

            // Build new container for this state
            var stateContainer = BuildStateContainer(parentContainer, activeModules);

            // Execute OnModuleEnter methods
            if (arrivingModules.Count > 0)
            {
                var moduleEnterMethods = BuildOrderedMethodList(
                    StateMethodSchedule.OnModuleEnter,
                    arrivingModules,
                    Array.Empty<Identification>(),
                    stateContainer);
                ExecuteOrderedMethods(moduleEnterMethods);
            }

            // Execute OnStateEnter methods
            var stateEnterMethods = BuildOrderedMethodList(
                StateMethodSchedule.OnStateEnter,
                activeModules,
                new[] { stateId },
                stateContainer);
            ExecuteOrderedMethods(stateEnterMethods);

            // Build PerFrame method list for this state
            var perFrameMethods = BuildOrderedMethodList(
                StateMethodSchedule.PerFrame,
                activeModules,
                new[] { stateId },
                stateContainer);

            // Push new state frame
            _stateStack.Push(new ActiveStateFrame(stateId, stateContainer, perFrameMethods));
        }

        Log.Information("State transition complete: now in state {CurrentState}", targetStateId);
    }

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        // Extract metadata using static abstract interface members
        var moduleMetadata = new ModuleMetadata(
            TStateModule.Identification,
            TStateModule.UsedServices,
            typeof(TStateModule));

        _modules[id] = moduleMetadata;

        Log.Debug("Registered module {ModuleId} ({ModuleType})", id, typeof(TStateModule).Name);
    }

    public void RemoveStateModule(Identification id)
    {
        if (_modules.Remove(id))
        {
            Log.Debug("Unregistered module {ModuleId}", id);
        }
    }

    public void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        // Extract metadata using static abstract interface members
        var parentId = TStateDescriptor.ParentId;
        var modules = TStateDescriptor.Modules;

        // Validate parent exists (except for root state)
        if (!parentId.Equals(Identification.Empty) && !_states.ContainsKey(parentId))
        {
            throw new InvalidOperationException(
                $"Cannot register state {id}: parent state {parentId} not found");
        }

        var stateMetadata = new StateMetadata(
            TStateDescriptor.Identification,
            parentId,
            modules,
            typeof(TStateDescriptor));

        _states[id] = stateMetadata;

        Log.Debug("Registered state {StateId} ({StateType}) with parent {ParentId}",
            id, typeof(TStateDescriptor).Name, parentId);
    }

    public void RemoveStateDescriptor(Identification id)
    {
        if (_states.Remove(id))
        {
            Log.Debug("Unregistered state {StateId}", id);
        }
    }
}
