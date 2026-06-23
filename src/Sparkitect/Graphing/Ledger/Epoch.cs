using JetBrains.Annotations;

namespace Sparkitect.Graphing.Ledger;

/// <summary>
/// A symbolic position in a resource's intra-frame dataflow. An epoch is plan structure, not a
/// resolved integer at collect time — authored code never sees an epoch number, and resolution to
/// concrete positions happens during the Link phase. Epochs are advanced only by declared
/// increments and are identical in shape every frame.
/// </summary>
/// <remarks>
/// The base epoch is a resource's introduced-but-unfilled state: it may be held (it is the input
/// of the first increment) but never read, because it has no producing increment.
/// </remarks>
[PublicAPI]
public readonly struct Epoch : IEquatable<Epoch>
{
    /// <summary>The symbolic step within this epoch's resource chain; the base epoch is step 0.</summary>
    private readonly int _step;

    private Epoch(int step) => _step = step;

    /// <summary>The introduced-but-unfilled base epoch (step 0) — holdable, never readable.</summary>
    public static Epoch Base => new(0);

    /// <summary>The symbolic step of this epoch within its resource's chain (0 is the base epoch).</summary>
    public int Step => _step;

    /// <summary>True when this is the base epoch — the only epoch with no producing increment.</summary>
    public bool IsBase => _step == 0;

    /// <summary>The next symbolic epoch produced by incrementing this one.</summary>
    public Epoch Next() => new(_step + 1);

    /// <inheritdoc/>
    public bool Equals(Epoch other) => _step == other._step;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Epoch other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _step;

    public static bool operator ==(Epoch left, Epoch right) => left.Equals(right);

    public static bool operator !=(Epoch left, Epoch right) => !left.Equals(right);

    public override string ToString() => IsBase ? "epoch:base" : $"epoch:{_step}";
}
