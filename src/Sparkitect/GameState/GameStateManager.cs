using JetBrains.Annotations;
using Serilog;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState.Samples;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Scaffolding implementation. The full logic will be wired with SG, registries, and DI.
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager
{
    private Identification? _currentStateId;

    public void Request(Identification stateId, object? payload = null)
    {
        Log.Information("GameState request: {StateId}", stateId);
        // Transition orchestration will be implemented in a later pass.
        _currentStateId = stateId;
        _ = payload; // placeholder
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

