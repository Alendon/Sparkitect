using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using OneOf;
using OneOf.Types;
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
    private ICoreContainer _currentContainer;
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

            foreach (var (key, wrapperType, schedule) in mappings)
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
        //Function needs probably a different (generic) signature for a type safe payload
        throw new NotImplementedException();
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
