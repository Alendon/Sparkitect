using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Data-flow ordering derives the topological order from Read/Increment edges alone — there is no
/// explicit ordering input. Asserts the emitted order respects every relation.
/// </summary>
public class OrderingTests
{
    private static readonly Identification OrderingMoment = Identification.Create(9, 1, 1);

    [Test]
    public async Task MomentReference_OrdersConsumerAfterProducerIncrement()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Declare the consumer FIRST so the mint-order tiebreak does NOT accidentally place it after
        // the producer: the ONLY thing that can order the consumer after the marked increment is a
        // real moment-derived ordering edge. Consumer-first therefore makes this red pre-fix.
        var consume = tx.Declare(new ConsumeMomentDescription(OrderingMoment, Size: 8));
        tx.Declare(new ProduceMomentDescription(OrderingMoment, Size: 16));

        var plan = await AssertOk(new GraphCompiler(ledger).Link());
        var order = plan.OrderedNodes.ToList();

        // The producer increment is the single marked node; the consume reader is the consume
        // description's minted node (the node ReferenceMoment recorded the read against).
        var producerIncrement = ledger.Nodes.Single(node => node.IsMarked).Id;
        var consumeReader = consume.Resource;

        // A cross-pass moment reference must derive produce-before-consume ordering (SC#3 / D-15).
        await Assert.That(order.IndexOf(consumeReader))
            .IsGreaterThan(order.IndexOf(producerIncrement));
    }
    [Test]
    public async Task LinearChain_OrdersReaderAfterProducer_AndIncrementAfterSource()
    {
        var ledger = CompileFixtures.LinearChain(out var producedA, out var readerB);

        var result = new GraphCompiler(ledger).Link();

        var plan = await AssertOk(result);
        var order = plan.OrderedNodes.ToList();

        // Every ledger node is placed exactly once.
        await Assert.That(order).HasCount().EqualTo(ledger.Nodes.Count);
        await Assert.That(order.Distinct().Count()).IsEqualTo(ledger.Nodes.Count);

        // Every increment's produced node comes after its source epoch.
        foreach (var increment in ledger.Increments)
        {
            await Assert.That(order.IndexOf(increment.ProducedNode))
                .IsGreaterThan(order.IndexOf(increment.SourceNode));
        }

        // Every read's reader comes after the producing epoch it reads.
        foreach (var read in ledger.Reads)
        {
            await Assert.That(order.IndexOf(read.Reader))
                .IsGreaterThan(order.IndexOf(read.EpochNode));
        }

        // Spot the specific data-flow edge from the fixture: B reads A1, so B is ordered after A1.
        await Assert.That(order.IndexOf(readerB)).IsGreaterThan(order.IndexOf(producedA));
    }

    private static async Task<CompiledPlan> AssertOk(Result<CompiledPlan, CompileError> result)
    {
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Ok>();
        return ((Result<CompiledPlan, CompileError>.Ok)result).Value;
    }
}
