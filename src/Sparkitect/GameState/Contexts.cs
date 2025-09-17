using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[PublicAPI]
public sealed class TransitionContext
{
    public required Identification FromStateId { get; init; }
    public required Identification ToStateId { get; init; }
    public object? Payload { get; init; }
}

[PublicAPI]
public sealed class FeatureContext
{
    public required Identification StateId { get; init; }
}

