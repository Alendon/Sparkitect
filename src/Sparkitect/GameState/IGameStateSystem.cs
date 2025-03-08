using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IGameStateSystem
{
    IReadOnlyList<Identification> EnterBefore => Array.Empty<Identification>();
    IReadOnlyList<Identification> EnterAfter => Array.Empty<Identification>();
    IReadOnlyList<Identification> ExitBefore => Array.Empty<Identification>();
    IReadOnlyList<Identification> ExitAfter => Array.Empty<Identification>();
    IReadOnlyList<Identification> UpdateBefore => Array.Empty<Identification>();
    IReadOnlyList<Identification> UpdateAfter => Array.Empty<Identification>();

    void Enter();
    
    void Exit();
    
    void Update();
}