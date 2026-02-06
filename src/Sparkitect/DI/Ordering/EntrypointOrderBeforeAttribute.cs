namespace Sparkitect.DI.Ordering;

/// <summary>
/// Declares that the annotated entrypoint class should execute before <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entrypoint type that should execute after the annotated class.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EntrypointOrderBeforeAttribute<T> : Attribute, IEntrypointOrdering
{
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
public sealed class EntrypointOrderBeforeAttribute : Attribute, IEntrypointOrdering
{
    private readonly string _targetTypeName;

    /// <param name="targetFullTypeName">The full type name of the entrypoint that should execute after the annotated class.</param>
    public EntrypointOrderBeforeAttribute(string targetFullTypeName)
    {
        _targetTypeName = targetFullTypeName;
    }

    public void ApplyOrdering(IEntrypointOrderingBuilder builder)
    {
        builder.AddEdge(builder.CurrentTypeName, _targetTypeName);
    }
}
