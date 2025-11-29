using System.Numerics;
using System.Runtime.InteropServices;

namespace PongMod;

/// <summary>
/// Game state data structure - designed for push constants.
/// All positions normalized to [0,1] range.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PongGameData
{
    // Paddle positions (Y center, X is fixed at edges)
    public float LeftPaddleY;
    public float RightPaddleY;

    // Paddle dimensions
    public float PaddleWidth;
    public float PaddleHeight;

    // Ball state
    public Vector2 BallPosition;
    public Vector2 BallVelocity;
    public float BallRadius;

    // Scores
    public int LeftScore;
    public int RightScore;

    // Screen dimensions for compute shader
    public uint ScreenWidth;
    public uint ScreenHeight;

    public static PongGameData CreateDefault() => new()
    {
        LeftPaddleY = 0.5f,
        RightPaddleY = 0.5f,
        PaddleWidth = 0.02f,
        PaddleHeight = 0.15f,
        BallPosition = new Vector2(0.5f, 0.5f),
        BallVelocity = new Vector2(0.3f, 0.2f),
        BallRadius = 0.015f,
        LeftScore = 0,
        RightScore = 0,
        ScreenWidth = 800,
        ScreenHeight = 600
    };
}
