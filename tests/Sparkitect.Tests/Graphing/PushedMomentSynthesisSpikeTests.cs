using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// A chain-head producer that marks a moment on its own birth increment — with no consuming pass
/// authoring the mark — binds that moment and orders its readers at the L1 core. This is the shape a
/// frame-start synthesized producer takes: a resource whose first (birth) increment carries the moment,
/// referenced by a separate reader. Proven both through the transaction grammar
/// (<c>tx.Increment(tx.Self, moment)</c>) and through direct ledger synthesis (mint/record into the
/// ledger with no description), since a synthesizer emits the marked increment straight into the ledger.
/// </summary>
public class PushedMomentSynthesisSpikeTests
{
    private static readonly Identification PushedMoment = Identification.Create(55, 4, 3);
    private static readonly Identification Provenance = Identification.Create(55, 4, 1);

    [Test]
    public async Task SynthesizedBirthIncrement_ViaTransaction_BindsMomentAndOrdersReaderAfterProducer()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Declare the reader FIRST so mint-order cannot masquerade as the ordering: the only thing that
        // can place the reader after the producer is a real moment-derived edge.
        var reader = tx.Declare(new ConsumeMomentDescription(PushedMoment, Size: 8));
        // The producer declares a resource and marks the moment on its own birth increment; no consuming
        // pass authors the mark — this stands in for a frame-start synthesized chain head.
        tx.Declare(new ProduceMomentDescription(PushedMoment, Size: 16));

        var plan = await AssertOk(new GraphCompiler(ledger).Link());

        // The moment binds to the single marked (synthesized) increment, not UndefinedMoment.
        await Assert.That(plan.ResolvedMoments).ContainsKey(PushedMoment);
        var markedNode = ledger.Nodes.Single(node => node.IsMarked);
        await Assert.That(plan.ResolvedMoments[PushedMoment].IncrementNode).IsEqualTo(markedNode.Id);

        // BuildOrderingGraph derives a producer-before-reader edge from the synthesized marked increment.
        var order = plan.OrderedNodes.ToList();
        await Assert.That(order.IndexOf(reader.Resource))
            .IsGreaterThan(order.IndexOf(markedNode.Id));
    }

    [Test]
    public async Task SynthesizedBirthIncrement_DirectLedgerSynthesis_BindsMomentAndOrdersReaderAfterProducer()
    {
        // Model the synthesizer emitting a marked chain head straight into the ledger (no description):
        // declare the reader resource first, then declare a producer resource, advance it one epoch to
        // its birth increment, and mark that increment with the pushed moment.
        var ledger = new DeclarationLedger();
        var reader = ledger.Declare<SyntheticComposite>(Provenance);

        var producer = ledger.Declare<SyntheticBuffer>(Provenance);
        var birth = ledger.RecordIncrement(producer, Provenance);
        ledger.RecordMoment(birth, PushedMoment);

        // A separate reader references the moment (the synthesized chain head has no author but the mark).
        ledger.RecordMomentRead(PushedMoment, reader.Resource);

        var plan = await AssertOk(new GraphCompiler(ledger).Link());

        // The moment binds to the synthesized birth increment (the chain's produced, post-base node).
        await Assert.That(plan.ResolvedMoments).ContainsKey(PushedMoment);
        var birthNode = ledger.ChainFor(producer.Resource)[1].Id;
        await Assert.That(plan.ResolvedMoments[PushedMoment].IncrementNode).IsEqualTo(birthNode);
        await Assert.That(plan.ResolvedMoments[PushedMoment].ResultEpoch.IsBase).IsFalse();

        // The reader (declared first) is ordered after the synthesized producer increment.
        var order = plan.OrderedNodes.ToList();
        await Assert.That(order.IndexOf(reader.Resource)).IsGreaterThan(order.IndexOf(birthNode));
    }

    private static async Task<CompiledPlan> AssertOk(Result<CompiledPlan, CompileError> result)
    {
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Ok>();
        return ((Result<CompiledPlan, CompileError>.Ok)result).Value;
    }
}
