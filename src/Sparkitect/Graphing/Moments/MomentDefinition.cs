using JetBrains.Annotations;

namespace Sparkitect.Graphing.Moments;

/// <summary>
/// Non-generic base of a moment definition, exposing the moment's resource type without the caller
/// knowing its generic argument. The link stage reads <see cref="ResourceType"/> off a registered
/// definition to learn what a moment carries; registration speaks the typed <see cref="MomentDefinition{T}"/>.
/// </summary>
[PublicAPI]
public abstract class MomentDefinition
{
    private protected MomentDefinition()
    {
    }

    /// <summary>The resource type this moment carries across passes.</summary>
    public abstract Type ResourceType { get; }
}

/// <summary>
/// The typed carrier a moment registration speaks: it conveys the moment's resource type <typeparamref name="T"/>
/// at registration time and nothing else (a moment declares name + resource type only — never backing,
/// position, or producer). The registry method's value parameter is this carrier; the link stage reads
/// <typeparamref name="T"/> off it via <see cref="MomentDefinition.ResourceType"/>.
/// </summary>
/// <typeparam name="T">The resource type the moment carries.</typeparam>
[PublicAPI]
public sealed class MomentDefinition<T> : MomentDefinition
{
    /// <inheritdoc/>
    public override Type ResourceType => typeof(T);
}
