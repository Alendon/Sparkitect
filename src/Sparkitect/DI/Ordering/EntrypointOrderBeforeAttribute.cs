using JetBrains.Annotations;

namespace Sparkitect.DI.Ordering;

/// <summary>
/// Declares that the annotated entrypoint class should execute before <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entrypoint type that should execute after the annotated class.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[PublicAPI]
public sealed class EntrypointOrderBeforeAttribute<T> : Attribute, IEntrypointOrdering
{
    /// <summary>Adds the ordering edge that places the annotated class before <typeparamref name="T"/>.</summary>
    /// <param name="builder">The ordering graph builder for the current entrypoint.</param>
    public void ApplyOrdering(IEntrypointOrderingBuilder builder)
    {
        builder.AddEdge(builder.CurrentTypeName, typeof(T).FullName!);
    }
}

/// <summary>
/// String-based variant for cross-mod ordering where the target type is not available at compile time.
/// Declares that the annotated entrypoint class should execute before the named type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[PublicAPI]
public sealed class EntrypointOrderBeforeAttribute : Attribute, IEntrypointOrdering
{
    private readonly string _targetTypeName;

    /// <param name="targetFullTypeName">The full type name of the entrypoint that should execute after the annotated class.</param>
    public EntrypointOrderBeforeAttribute(string targetFullTypeName)
    {
        _targetTypeName = targetFullTypeName;
    }

    /// <summary>Adds the ordering edge that places the annotated class before the named type.</summary>
    /// <param name="builder">The ordering graph builder for the current entrypoint.</param>
    public void ApplyOrdering(IEntrypointOrderingBuilder builder)
    {
        builder.AddEdge(builder.CurrentTypeName, _targetTypeName);
    }
}
