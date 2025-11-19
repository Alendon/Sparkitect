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
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade, IGameStateManagerStateFacade
{
    private readonly Dictionary<Identification, StateMetadata> _registeredStates = new();
    private readonly Dictionary<Identification, ModuleMetadata> _registeredModules = new();
    private readonly Stack<ActiveStateFrame> _stateStack = new();

    private readonly List<Func<StateMetadata>> _pendingStates = new();
    private readonly List<Func<ModuleMetadata>> _pendingModules = new();

    private IReadOnlyDictionary<(Identification, string, StateMethodSchedule), Type>? _methodAssociations;
    private HashSet<OrderingEntry>? _methodOrdering;
    private IReadOnlyDictionary<Type, Type>? _facadeMap;

    private Identification? _pendingTransitionTarget;
    private bool _isTransitioning;

    public required IModManager ModManager { get; init; }

    public ICoreContainer CurrentCoreContainer => _stateStack.Count > 0 ? _stateStack.Peek().Container : RootContainer;
    public ICoreContainer RootContainer { get; set; } = null!;

    /// <summary>
    /// Enter the Root Game State. To be called by the Engine Bootstrapper
    /// </summary>
    public void EnterRootState()
    {

    }

    public void Request(Identification stateId, object? payload = null)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
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
        foreach (var moduleFactory in _pendingModules)
        {
            var metadata = moduleFactory();
            _registeredModules[metadata.Id] = metadata;
            Log.Debug("Finalized module registration: {ModuleId}", metadata.Id);
        }
        _pendingModules.Clear();

        foreach (var stateFactory in _pendingStates)
        {
            var metadata = stateFactory();
            _registeredStates[metadata.Id] = metadata;
            Log.Debug("Finalized state registration: {StateId} (parent: {ParentId})", metadata.Id, metadata.ParentId);
        }
        _pendingStates.Clear();

        Log.Information("Finalized {ModuleCount} modules and {StateCount} states",
            _registeredModules.Count, _registeredStates.Count);
    }
}
