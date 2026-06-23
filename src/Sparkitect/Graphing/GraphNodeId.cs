using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>
/// Value identity of a ledger node, minted when a declaration is recorded — never derived from
/// location, naming, or declaration order. Provenance lives as metadata on the node, not in this
/// identity. Graph services key on it for dependencies, diagnostics, and tooling; it is not
/// author-facing.
/// </summary>
[PublicAPI]
public readonly struct GraphNodeId : IEquatable<GraphNodeId>
{
    private readonly int _value;

    private GraphNodeId(int value) => _value = value;

    /// <summary>The unset identity; never minted by the ledger.</summary>
    public static GraphNodeId None => default;

    /// <summary>True when this is the unset identity.</summary>
    public bool IsNone => _value == 0;

    /// <summary>
    /// Mints the node identity for the given monotonically increasing ordinal. Minting is the
    /// ledger's responsibility; ordinals are an internal counter, not a stable external address.
    /// </summary>
    internal static GraphNodeId Mint(int ordinal) => new(ordinal + 1);

    /// <inheritdoc/>
    public bool Equals(GraphNodeId other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GraphNodeId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value;

    public static bool operator ==(GraphNodeId left, GraphNodeId right) => left.Equals(right);

    public static bool operator !=(GraphNodeId left, GraphNodeId right) => !left.Equals(right);

    public override string ToString() => IsNone ? "node:none" : $"node:{_value - 1}";
}
