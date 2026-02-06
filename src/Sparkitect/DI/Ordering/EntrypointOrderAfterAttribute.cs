namespace Sparkitect.DI.Ordering;

/// <summary>
/// Declares that the annotated entrypoint class should execute after <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entrypoint type that should execute before the annotated class.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EntrypointOrderAfterAttribute<T> : Attribute, IEntrypointOrdering
{
    public void ApplyOrdering(IEntrypointOrderingBuilder builder)
    {
        builder.AddEdge(typeof(T).FullName!, builder.CurrentTypeName);
    }
}

/// <summary>
/// String-based variant for cross-mod ordering where the target type is not available at compile time.
/// Declares that the annotated entrypoint class should execute after the named type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EntrypointOrderAfterAttribute : Attribute, IEntrypointOrdering
{
    private readonly string _targetTypeName;

    /// <param name="targetFullTypeName">The full type name of the entrypoint that should execute before the annotated class.</param>
    public EntrypointOrderAfterAttribute(string targetFullTypeName)
    {
        _targetTypeName = targetFullTypeName;
    }

    public void ApplyOrdering(IEntrypointOrderingBuilder builder)
    {
        builder.AddEdge(_targetTypeName, builder.CurrentTypeName);
    }
}
