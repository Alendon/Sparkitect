using System.Numerics;
using System.Runtime.InteropServices;

namespace PongMod;

/// <summary>Game state, laid out for push constants; positions are normalized to [0,1].</summary>
[StructLayout(LayoutKind.Sequential)]
public struct PongGameData
{
    // Only paddle Y is stored; X is fixed at the edges.
    public float LeftPaddleY;
    public float RightPaddleY;

    public float PaddleWidth;
    public float PaddleHeight;

    public Vector2 BallPosition;
    public Vector2 BallVelocity;
    public float BallRadius;

    public Vector3 BackgroundColor;

    public int LeftScore;
    public int RightScore;

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
        BackgroundColor = new Vector3(0.1f, 0.1f, 0.15f),
        LeftScore = 0,
        RightScore = 0,
        ScreenWidth = 800,
        ScreenHeight = 600
    };
}
