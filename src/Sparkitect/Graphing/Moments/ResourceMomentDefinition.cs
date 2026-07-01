using JetBrains.Annotations;

namespace Sparkitect.Graphing.Moments;

/// <summary>Non-generic base of a resource-moment definition, exposing the moment's resource type without the caller knowing its generic argument. A resource moment is a cross-pass mark on an epoched resource increment, not a pass-level moment.</summary>
[PublicAPI]
public abstract class ResourceMomentDefinition
{
    private protected ResourceMomentDefinition()
    {
    }

    /// <summary>The resource type this moment carries across passes.</summary>
    public abstract Type ResourceType { get; }
}

/// <summary>The typed carrier a resource-moment registration speaks, conveying the moment's resource type <typeparamref name="T"/> and nothing else (never backing, position, or producer).</summary>
/// <typeparam name="T">The resource type the moment carries.</typeparam>
[PublicAPI]
public sealed class ResourceMomentDefinition<T> : ResourceMomentDefinition
{
    /// <inheritdoc/>
    public override Type ResourceType => typeof(T);
}
