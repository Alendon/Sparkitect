using System.Numerics;
using Sparkitect.GameState;

namespace PongMod;

public interface IPongRuntimeService
{
    ref PongGameData GameData { get; }
    float DeltaTime { get; }

    /// <summary>
    /// Gets or sets the background clear color for rendering.
    /// Default: dark gray-blue (0.1, 0.1, 0.15).
    /// </summary>
    Vector3 BackgroundColor { get; set; }

    void Initialize();
    void Tick();
    void Render();
    void Cleanup();
    void MoveLeftPaddle(float delta);
    void MoveRightPaddle(float delta);
    void ResetBall();
}