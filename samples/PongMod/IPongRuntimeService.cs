using System.Numerics;
using Sparkitect.GameState;
using Sparkitect.Windowing;

namespace PongMod;

public interface IPongRuntimeService
{
    ref PongGameData GameData { get; }
    float DeltaTime { get; }

    /// <summary>
    /// The mod-owned window. Exposed so passes and the shared-image registration can read
    /// <c>Window.Swapchain.Extent</c>.
    /// </summary>
    ISparkitWindow Window { get; }

    /// <summary>Whether the window is still open.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets or sets the background clear color for rendering.
    /// Default: dark gray-blue (0.1, 0.1, 0.15).
    /// </summary>
    Vector3 BackgroundColor { get; set; }

    void Initialize();

    /// <summary>Registers the shared render target at the live extent and builds the render graph.</summary>
    void CreateGraph();

    /// <summary>Pumps window events.</summary>
    void PollWindow();

    /// <summary>Drives one render-graph frame (acquire/submit/present owned by the graph).</summary>
    void RunFrame();

    /// <summary>Tears down the render graph.</summary>
    void ShutdownGraph();

    void Tick();
    void Cleanup();
    void MoveLeftPaddle(float delta);
    void MoveRightPaddle(float delta);
    void ResetBall();
}
