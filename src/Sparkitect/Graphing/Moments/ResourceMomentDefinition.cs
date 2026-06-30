using JetBrains.Annotations;

namespace Sparkitect.Graphing.Moments;

/// <summary>
/// Non-generic base of a resource-moment definition, exposing the moment's resource type without the
/// caller knowing its generic argument. The link stage reads <see cref="ResourceType"/> off a registered
/// definition to learn what a moment carries; registration speaks the typed
/// <see cref="ResourceMomentDefinition{T}"/>.
/// </summary>
/// <remarks>
/// A "resource moment" is a cross-pass mark on an epoched resource increment — NOT a logical/pass moment.
/// The name carries <c>Resource</c> explicitly so a reader does not confuse it with pass-level moments.
/// </remarks>
[PublicAPI]
public abstract class ResourceMomentDefinition
{
    private protected ResourceMomentDefinition()
    {
    }

    /// <summary>The resource type this moment carries across passes.</summary>
    public abstract Type ResourceType { get; }
}

/// <summary>
/// The typed carrier a resource-moment registration speaks: it conveys the moment's resource type
/// <typeparamref name="T"/> at registration time and nothing else (a moment declares name + resource
/// type only — never backing, position, or producer). The registry method's value parameter is this
/// carrier; the link stage reads <typeparamref name="T"/> off it via
/// <see cref="ResourceMomentDefinition.ResourceType"/>.
/// </summary>
/// <typeparam name="T">The resource type the moment carries.</typeparam>
[PublicAPI]
public sealed class ResourceMomentDefinition<T> : ResourceMomentDefinition
{
    /// <inheritdoc/>
    public override Type ResourceType => typeof(T);
}
