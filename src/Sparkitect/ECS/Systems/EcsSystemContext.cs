using JetBrains.Annotations;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Contextual data supplied to ECS system functions. Carries the <see cref="IWorld"/> the system runs against.
/// </summary>
[PublicAPI]
public sealed class EcsSystemContext
{
    /// <summary>The world the system function operates on.</summary>
    public required IWorld World { get; init; }
}
