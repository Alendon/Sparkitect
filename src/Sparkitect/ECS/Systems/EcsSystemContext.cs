using JetBrains.Annotations;

namespace Sparkitect.ECS.Systems;

[PublicAPI]
public sealed class EcsSystemContext
{
    public required IWorld World { get; init; }
}
