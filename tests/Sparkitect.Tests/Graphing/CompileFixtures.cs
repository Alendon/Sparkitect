using Sparkitect.Graphing;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Builders that assemble synthetic ledgers directly via the Plan-02 <see cref="DeclarationLedger"/>
/// mint/record API for the compile oracle suites. These build diagnostic ledgers at the ledger level
/// (they do not extend <c>SyntheticDomain</c>), keeping the compile suites parallel-safe with the
/// transaction work. A reader is itself a graph node, so each builder obtains a real reader identity
/// from a declaring node rather than inventing a contrived id.
/// </summary>
internal static class CompileFixtures
{
    private static readonly Identification Provenance = Identification.Create(1, 1, 1);

    /// <summary>
    /// A valid linear chain: declare A, increment it once (A1), declare a reader B that reads A1,
    /// then increment B once. Yields a DAG whose data-flow order is fully determined by the relations.
    /// </summary>
    internal static DeclarationLedger LinearChain(out GraphNodeId producedA, out GraphNodeId readerB)
    {
        var ledger = new DeclarationLedger();
        var a = ledger.Declare<SyntheticBuffer>(Id(1, 1, 1));
        var a1 = ledger.RecordIncrement(a, Id(1, 2, 1));

        var b = ledger.Declare<SyntheticComposite>(Id(1, 1, 2));
        ledger.RecordRead(a1, b.Resource);
        ledger.RecordIncrement(b, Id(1, 2, 2));

        producedA = ledger.ChainFor(a.Resource)[1].Id;
        readerB = b.Resource;
        return ledger;
    }

    /// <summary>
    /// A fork: declare A, increment it once to A1, then increment A1 twice from the same source epoch.
    /// The two competing increments are structurally inexpressible — the canonical Fork diagnostic.
    /// </summary>
    internal static DeclarationLedger Fork(out GraphNodeId sharedSource)
    {
        var ledger = new DeclarationLedger();
        var a = ledger.Declare<SyntheticBuffer>(Id(2, 1, 1));
        var a1 = ledger.RecordIncrement(a, Id(2, 2, 1));

        // Two increments both advancing the SAME source epoch (a1) — a fork.
        ledger.RecordIncrement(a1, Id(2, 2, 2));
        ledger.RecordIncrement(a1, Id(2, 2, 3));

        sharedSource = ledger.ChainFor(a.Resource)[1].Id;
        return ledger;
    }

    /// <summary>
    /// An unproducible read: declare A and read its base epoch directly (the base epoch has no
    /// producing increment, so it is unschedulable by construction).
    /// </summary>
    internal static DeclarationLedger UnproducibleBaseRead(out GraphNodeId reader, out GraphNodeId baseEpoch)
    {
        var ledger = new DeclarationLedger();
        var a = ledger.Declare<SyntheticBuffer>(Id(3, 1, 1));
        var b = ledger.Declare<SyntheticComposite>(Id(3, 1, 2));

        // Read A at its base epoch — never produced.
        ledger.RecordRead(a, b.Resource);

        reader = b.Resource;
        baseEpoch = ledger.ChainFor(a.Resource)[0].Id;
        return ledger;
    }

    /// <summary>
    /// A cycle: two resources whose reads close a loop through their increment chains.
    /// A0->A1, B0->B1 (increments); A1 read by B (A1->B0), B1 read by A (B1->A0).
    /// The derived edges form A0->A1->B0->B1->A0.
    /// </summary>
    internal static DeclarationLedger Cycle()
    {
        var ledger = new DeclarationLedger();
        var a = ledger.Declare<SyntheticBuffer>(Id(4, 1, 1));
        var b = ledger.Declare<SyntheticBuffer>(Id(4, 1, 2));

        var a1 = ledger.RecordIncrement(a, Id(4, 2, 1));
        var b1 = ledger.RecordIncrement(b, Id(4, 2, 2));

        // A1 is read by B's node, B1 is read by A's node — mutual data-flow dependency.
        ledger.RecordRead(a1, b.Resource);
        ledger.RecordRead(b1, a.Resource);

        return ledger;
    }

    private static Identification Id(ushort mod, ushort cat, uint item) => Identification.Create(mod, cat, item);
}
