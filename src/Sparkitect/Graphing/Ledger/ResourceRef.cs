using JetBrains.Annotations;

namespace Sparkitect.Graphing.Ledger;

/// <summary>An opaque, typed, epoch-qualified structural reference to a resource at one symbolic epoch. Distinct from <see cref="IGraphResource{T}"/>: description-internal wiring that lives inside facts, never fetched. Minted only by ledger transaction verbs.</summary>
/// <typeparam name="T">The resource type the reference points at, carried by C# generic shape.</typeparam>
[PublicAPI]
public readonly struct ResourceRef<T> : IEquatable<ResourceRef<T>>
{
    /// <summary>Identity of the resource chain this reference belongs to (its declaring node).</summary>
    internal GraphNodeId Resource { get; }

    /// <summary>The symbolic epoch this reference is qualified to.</summary>
    internal Epoch Epoch { get; }

    /// <summary>Minted only by <see cref="DeclarationLedger"/>; never publicly constructible.</summary>
    internal ResourceRef(GraphNodeId resource, Epoch epoch)
    {
        Resource = resource;
        Epoch = epoch;
    }

    /// <summary>True when this reference points at its resource's holdable, never-readable base epoch.</summary>
    public bool IsBaseEpoch => Epoch.IsBase;

    /// <inheritdoc/>
    public bool Equals(ResourceRef<T> other) => Resource == other.Resource && Epoch == other.Epoch;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ResourceRef<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Resource, Epoch);

    public static bool operator ==(ResourceRef<T> left, ResourceRef<T> right) => left.Equals(right);

    public static bool operator !=(ResourceRef<T> left, ResourceRef<T> right) => !left.Equals(right);

    public override string ToString() => $"ref<{typeof(T).Name}>({Resource}@{Epoch})";
}
