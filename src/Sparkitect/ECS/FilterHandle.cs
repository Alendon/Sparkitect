using JetBrains.Annotations;

namespace Sparkitect.ECS;

/// <summary>
/// Handle identifying a registered capability filter within a <see cref="World"/>.
/// Filters are long-lived and do not use generational validation -- they are never removed.
/// </summary>
[PublicAPI]
public readonly struct FilterHandle : IEquatable<FilterHandle>
{
    /// <summary>
    /// Index into the World's filter registry.
    /// </summary>
    public readonly int Index;

    internal FilterHandle(int index)
    {
        Index = index;
    }

    /// <inheritdoc/>
    public bool Equals(FilterHandle other)
    {
        return Index == other.Index;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is FilterHandle other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Filter({Index})";
    }

    /// <summary>Returns true when both handles reference the same filter index.</summary>
    public static bool operator ==(FilterHandle left, FilterHandle right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns true when the handles reference different filter indices.</summary>
    public static bool operator !=(FilterHandle left, FilterHandle right)
    {
        return !left.Equals(right);
    }
}
