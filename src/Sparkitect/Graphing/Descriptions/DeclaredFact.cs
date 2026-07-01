using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>The non-generic base of a declared fact, carrying its <see cref="CleanupStrategy"/>.</summary>
[PublicAPI]
public interface DeclaredFact
{
    /// <summary>How this fact's resolved instance is released when the graph tears down.</summary>
    public CleanupStrategy CleanupStrategy { get; }
}

/// <summary>
/// The immutable facts of a declaration: it owns construction of the live <typeparamref name="T"/>
/// instance from an <see cref="IInstanceContext"/>.
/// </summary>
[PublicAPI]
public interface DeclaredFact<T> : DeclaredFact
{
    /// <summary>Builds the live instance, resolving any sub-references through <paramref name="ctx"/>.</summary>
    public T CreateInstance(IInstanceContext ctx);
}
