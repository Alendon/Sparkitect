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
    }


    public void Request(Identification stateId, object? payload = null)
    {
        //Function needs probably a different (generic) signature for a type safe payload
        throw new NotImplementedException();
    }

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        throw new NotImplementedException();
    }

    public void RemoveStateModule(Identification id)
    {
        throw new NotImplementedException();
    }

    public void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        throw new NotImplementedException();
    }

    public void RemoveStateDescriptor(Identification id)
    {
        throw new NotImplementedException();
    }
}
