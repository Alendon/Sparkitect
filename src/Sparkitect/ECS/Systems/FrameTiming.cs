using JetBrains.Annotations;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Value-type carrier for per-frame timing data.
/// Passed to <see cref="ISystemManager.ExecuteSystems"/> each frame.
/// </summary>
[PublicAPI]
public readonly struct FrameTiming(float deltaTime, float totalTime)
{
    /// <summary>
    /// Time elapsed since the previous frame, in seconds.
    /// </summary>
    public float DeltaTime { get; } = deltaTime;

    /// <summary>
    /// Total elapsed time since the simulation started, in seconds.
    /// </summary>
    public float TotalTime { get; } = totalTime;
}
