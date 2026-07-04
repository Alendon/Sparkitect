using JetBrains.Annotations;

namespace Sparkitect.Utils.Ordering;

/// <summary>
/// Selects the deterministic order in which ready (zero-remaining-dependency) nodes are drained
/// during a topological sort. Two canned strategies ship: <see cref="InsertionOrder"/> drains by
/// the order nodes were added to the builder, and <see cref="Lexicographic"/> drains by an injected
/// comparer. The comparer is supplied per instantiation so node keys need not be
/// <see cref="IComparable"/>.
/// </summary>
/// <typeparam name="TNode">The graph node key type.</typeparam>
[PublicAPI]
public sealed class OrderingTiebreak<TNode>
{
    private OrderingTiebreak(IComparer<TNode>? comparer) => Comparer = comparer;

    /// <summary>
    /// Drains ready nodes in the order they were added via
    /// <c>OrderingGraphBuilder&lt;TNode&gt;.AddNode</c>. This is the stable, add-order tiebreak.
    /// </summary>
    public static OrderingTiebreak<TNode> InsertionOrder { get; } = new(null);

    /// <summary>
    /// Drains ready nodes by the supplied <paramref name="comparer"/>, smallest first. Pass
    /// <see cref="StringComparer.Ordinal"/> (for string keys) to reproduce ordinal-lexicographic order.
    /// </summary>
    /// <param name="comparer">The comparer that ranks ready nodes; must not be null.</param>
    /// <returns>A lexicographic tiebreak strategy driven by <paramref name="comparer"/>.</returns>
    public static OrderingTiebreak<TNode> Lexicographic(IComparer<TNode> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        return new OrderingTiebreak<TNode>(comparer);
    }

    /// <summary>
    /// The comparer that ranks ready nodes, or null for insertion-order draining.
    /// Read by <c>OrderingGraphBuilder&lt;TNode&gt;.Sort</c> to select the drain algorithm.
    /// </summary>
    internal IComparer<TNode>? Comparer { get; }
}
