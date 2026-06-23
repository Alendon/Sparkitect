using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The immutable outcome of a description's declaration: the references it minted/sub-declared, plus
/// ownership of instance creation. Facts are graph-side truth — they cannot consult the description
/// that produced them and build the live instance only from references resolved through the
/// instance context (dependency-first).
/// </summary>
/// <typeparam name="T">The resource type the built instance is.</typeparam>
[PublicAPI]
public abstract record DeclaredFacts<T>
{
    /// <summary>
    /// Builds the live instance for the frame, resolving each held reference to its concrete
    /// dependency via <paramref name="ctx"/> (dependency-first, N=1 single in-flight frame). The
    /// facts compose <typeparamref name="T"/> from the resolved sub-instances; they never reach back
    /// to the description.
    /// </summary>
    public abstract T CreateInstance(IInstanceContext ctx);
}
