using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Scaffolding implementation. The full logic will be wired with SG, registries, and DI.
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager
{
    private Identification? _currentStateId;
    private readonly Dictionary<Identification, StateModuleRegistration> _modules = new();
    private readonly Dictionary<Identification, StateRegistration> _states = new();

    public void Request(Identification stateId, object? payload = null)
    {
        if (!_states.TryGetValue(stateId, out var state))
        {
            Log.Warning("Requested state {StateId} is not registered", stateId);
            return;
        }

        Log.Information("GameState request: {StateId}", stateId);

        _currentStateId = stateId;
        _ = payload; // placeholder for future payload handling
    }

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        var registration = new StateModuleRegistration(typeof(TStateModule));

        if (_modules.TryGetValue(id, out var existing))
        {
            if (existing.ModuleType != registration.ModuleType)
            {
                Log.Warning("State module id {Id} is already registered with {ExistingType}, replacing with {NewType}",
                    id, existing.ModuleType, registration.ModuleType);
            }
        }

        _modules[id] = registration;
    }

    public void RemoveStateModule(Identification id)
    {
        if (!_modules.Remove(id))
        {
            Log.Warning("Attempted to remove unknown state module {Id}", id);
        }
    }

    public void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        var modules = TStateDescriptor.Modules ?? Array.Empty<Identification>();
        var activation = TStateDescriptor.Activation;

        var registration = new StateRegistration(
            typeof(TStateDescriptor),
            TStateDescriptor.ParentId,
            modules,
            activation);

        if (_states.TryGetValue(id, out var existing))
        {
            if (existing.DescriptorType != registration.DescriptorType)
            {
                Log.Warning("State id {Id} is already registered with {ExistingType}, replacing with {NewType}",
                    id, existing.DescriptorType, registration.DescriptorType);
            }
        }

        _states[id] = registration;
    }

    public void RemoveStateDescriptor(Identification id)
    {
        if (!_states.Remove(id))
        {
            Log.Warning("Attempted to remove unknown state {Id}", id);
        }
    }

    private sealed record StateModuleRegistration(Type ModuleType);

    private sealed record StateRegistration(
        Type DescriptorType,
        Identification ParentId,
        IReadOnlyList<Identification> Modules,
        IReadOnlyDictionary<Identification, StateActivationPolicy>? Activation);
}
