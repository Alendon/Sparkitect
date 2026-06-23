using System.Runtime.CompilerServices;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// <see cref="UnsafeAccessorAttribute"/> accessors for <see cref="DeclarationLedger"/> private
/// internals the oracle must inspect, with no production test-only seam. Used to assert minting
/// behavior (the node-ordinal counter) that the public query surface does not expose directly.
/// </summary>
internal static class LedgerInternalsAccessor
{
    /// <summary>The ledger's private node-ordinal counter (next ordinal to mint).</summary>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_nodeOrdinal")]
    internal static extern ref int NodeOrdinal(DeclarationLedger ledger);
}
