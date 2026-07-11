using System.Numerics;
using Sparkitect.GameState;
using Sparkitect.Windowing;

namespace PongMod;

public interface IPongRuntimeService
{
    ref PongGameData GameData { get; }
    float DeltaTime { get; }

    ISparkitWindow Window { get; }
    bool IsOpen { get; }
    Vector3 BackgroundColor { get; set; }

    void Initialize();
    void WireInput();
    void CreateGraph();

    /// <summary>Drives one render-graph frame; acquire/submit/present are owned by the graph.</summary>
    void RunFrame();

    void ShutdownGraph();

    void Tick();
    void Cleanup();
    void MoveLeftPaddle(float delta);
    void MoveRightPaddle(float delta);
    void ResetBall();
}
