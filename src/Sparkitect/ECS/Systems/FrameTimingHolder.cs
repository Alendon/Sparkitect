using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Mutable reference-type holder for <see cref="FrameTiming"/>.
/// Resolved once during wrapper Initialize and cached; its value is updated
/// every <see cref="ISystemManager.ExecuteSystems"/> call before any system executes.
/// Systems access timing via the delegated properties on this holder instance.
/// </summary>
[AllowConcreteResolution]
public class FrameTimingHolder
{
    private FrameTiming _current;

    /// <summary>
    /// Time elapsed since the previous frame, in seconds.
    /// </summary>
    public float DeltaTime => _current.DeltaTime;

    /// <summary>
    /// Total elapsed time since the simulation started, in seconds.
    /// </summary>
    public float TotalTime => _current.TotalTime;

    /// <summary>
    /// Updates the held timing data. Called by SystemManager before each execution loop.
    /// </summary>
    internal void Update(FrameTiming timing) => _current = timing;
}
