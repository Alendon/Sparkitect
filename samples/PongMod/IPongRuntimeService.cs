using Sparkitect.GameState;

namespace PongMod;

[StateFacade<IPongRuntimeServiceStateFacade>]
public interface IPongRuntimeService
{
    ref PongGameData GameData { get; }
    float DeltaTime { get; }

    void Initialize();
    void Tick();
    void Render();
    void Cleanup();
    void MoveLeftPaddle(float delta);
    void MoveRightPaddle(float delta);
    void ResetBall();
}

public interface IPongRuntimeServiceStateFacade;