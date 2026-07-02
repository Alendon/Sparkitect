using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>Value identity of a ledger node, minted when a declaration is recorded — never derived from location, naming, or declaration order. Not author-facing.</summary>
[PublicAPI]
public readonly struct GraphNodeId : IEquatable<GraphNodeId>
{
    private readonly int _value;

    private GraphNodeId(int value) => _value = value;

    /// <summary>The unset identity; never minted by the ledger.</summary>
    public static GraphNodeId None => default;

    /// <summary>True when this is the unset identity.</summary>
    public bool IsNone => _value == 0;

    internal static GraphNodeId Mint(int ordinal) => new(ordinal + 1);

    /// <inheritdoc/>
    public bool Equals(GraphNodeId other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GraphNodeId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value;

    /// <summary>Value equality over node identity.</summary>
    public static bool operator ==(GraphNodeId left, GraphNodeId right) => left.Equals(right);

    /// <summary>Value inequality over node identity.</summary>
    public static bool operator !=(GraphNodeId left, GraphNodeId right) => !left.Equals(right);

    /// <summary>Debug rendering as <c>node:none</c> or <c>node:{ordinal}</c>.</summary>
    public override string ToString() => IsNone ? "node:none" : $"node:{_value - 1}";
}
