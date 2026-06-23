using Sparkitect.Graphing;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Ledger Read/Increment + epoch-chain correctness oracle. Asserts on epoch-chain state and
/// node/edge structure (via the public query surface and the <c>[UnsafeAccessor]</c> internals),
/// never on exception-message substrings.
/// </summary>
public class LedgerTests
{
    private static readonly Identification LeafProvenance = Identification.Create(1, 1, 1);
    private static readonly Identification IncrementProvenance = Identification.Create(1, 2, 1);
    private static readonly Identification SecondLeafProvenance = Identification.Create(1, 1, 2);
    private static readonly Identification ReaderProvenance = Identification.Create(1, 3, 1);
    private static readonly Identification Moment = Identification.Create(7, 7, 7);

    [Test]
    public async Task Declare_MintsBaseEpochRef_NotReadableYet()
    {
        var ledger = new DeclarationLedger();

        var leaf = ledger.Declare<SyntheticBuffer>(LeafProvenance);

        await Assert.That(leaf.IsBaseEpoch).IsTrue();
        await Assert.That(ledger.Nodes).HasCount().EqualTo(1);

        var node = ledger.Nodes[0];
        await Assert.That(node.IsBaseEpoch).IsTrue();
        await Assert.That(node.ProducingIncrementSource).IsEqualTo(GraphNodeId.None);
        await Assert.That(node.ResourceType).IsEqualTo(typeof(SyntheticBuffer));
        await Assert.That(node.Provenance).IsEqualTo(LeafProvenance);
        // The mint counter advanced once (private-internals inspection, no production seam).
        await Assert.That(LedgerInternalsAccessor.NodeOrdinal(ledger)).IsEqualTo(1);
    }

    [Test]
    public async Task RecordIncrement_AdvancesEpochAndMintsDistinctRef()
    {
        var ledger = new DeclarationLedger();
        var leaf = ledger.Declare<SyntheticBuffer>(LeafProvenance);

        var produced = ledger.RecordIncrement(leaf, IncrementProvenance);

        // The epoch chain grew by one; the post-increment ref is distinct from the base ref.
        await Assert.That(produced).IsNotEqualTo(leaf);
        await Assert.That(produced.IsBaseEpoch).IsFalse();

        var chain = ledger.ChainFor(leaf.Resource);
        await Assert.That(chain).HasCount().EqualTo(2);
        await Assert.That(chain[0].IsBaseEpoch).IsTrue();
        await Assert.That(chain[1].IsBaseEpoch).IsFalse();

        // The increment edge records source -> produced provenance.
        await Assert.That(ledger.Increments).HasCount().EqualTo(1);
        var edge = ledger.Increments[0];
        await Assert.That(edge.SourceNode).IsEqualTo(chain[0].Id);
        await Assert.That(edge.ProducedNode).IsEqualTo(chain[1].Id);
        await Assert.That(chain[1].ProducingIncrementSource).IsEqualTo(chain[0].Id);
    }

    [Test]
    public async Task RecordRead_RegistersReaderEdgeAgainstProducedEpoch()
    {
        var ledger = new DeclarationLedger();
        var leaf = ledger.Declare<SyntheticBuffer>(LeafProvenance);
        var produced = ledger.RecordIncrement(leaf, IncrementProvenance);
        // A reader is itself a graph node; declare one to obtain a real reader identity.
        var reader = ledger.Declare<SyntheticComposite>(ReaderProvenance);

        ledger.RecordRead(produced, reader.Resource);

        await Assert.That(ledger.Reads).HasCount().EqualTo(1);
        var read = ledger.Reads[0];
        await Assert.That(read.Reader).IsEqualTo(reader.Resource);
        await Assert.That(read.Epoch).IsEqualTo(produced.Epoch);

        var producedNode = ledger.ChainFor(leaf.Resource)[1];
        await Assert.That(producedNode.Readers).Contains(reader.Resource);
    }

    [Test]
    public async Task TwoResources_MaintainIndependentEpochChains()
    {
        var ledger = new DeclarationLedger();
        var first = ledger.Declare<SyntheticBuffer>(LeafProvenance);
        var second = ledger.Declare<SyntheticBuffer>(SecondLeafProvenance);

        // Advance only the first resource twice; the second stays at its base epoch.
        var firstOnce = ledger.RecordIncrement(first, IncrementProvenance);
        ledger.RecordIncrement(firstOnce, IncrementProvenance);

        await Assert.That(first.Resource).IsNotEqualTo(second.Resource);
        await Assert.That(ledger.ChainFor(first.Resource)).HasCount().EqualTo(3);
        await Assert.That(ledger.ChainFor(second.Resource)).HasCount().EqualTo(1);
    }

    [Test]
    public async Task SelfIncrement_AndSubResourceIncrement_AreBothRecordableViaSameMechanic()
    {
        var ledger = new DeclarationLedger();

        // Sub-resource increment: a composite declares a sub-resource and advances it.
        var sub = ledger.Declare<SyntheticBuffer>(LeafProvenance);
        var stagedSub = ledger.RecordIncrement(sub, IncrementProvenance);

        // Self-increment: a description advances the resource it itself resolves to.
        var selfResolved = ledger.Declare<SyntheticComposite>(SecondLeafProvenance);
        var advancedSelf = ledger.RecordIncrement(selfResolved, IncrementProvenance);

        await Assert.That(stagedSub.IsBaseEpoch).IsFalse();
        await Assert.That(advancedSelf.IsBaseEpoch).IsFalse();
        // Both increments used the same ref-pointed mechanic — two independent chains, each grown.
        await Assert.That(ledger.ChainFor(sub.Resource)).HasCount().EqualTo(2);
        await Assert.That(ledger.ChainFor(selfResolved.Resource)).HasCount().EqualTo(2);
        await Assert.That(ledger.Increments).HasCount().EqualTo(2);
    }

    [Test]
    public async Task RecordMoment_MarksProducingIncrement()
    {
        var ledger = new DeclarationLedger();
        var leaf = ledger.Declare<SyntheticBuffer>(LeafProvenance);
        var produced = ledger.RecordIncrement(leaf, IncrementProvenance);

        ledger.RecordMoment(produced, Moment);

        var producedNode = ledger.ChainFor(leaf.Resource)[1];
        await Assert.That(producedNode.IsMarked).IsTrue();
        await Assert.That(producedNode.MarkedMoment).IsEqualTo(Moment);
    }
}
