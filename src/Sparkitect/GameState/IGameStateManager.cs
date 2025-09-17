using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[PublicAPI]
public interface IGameStateManager
{
    /// <summary>
    /// Request a transition to a target state by id with an optional payload.
    /// </summary>
    void Request(Identification stateId, object? payload = null);
}

