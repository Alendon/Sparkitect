using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Data-flow ordering derives the topological order from Read/Increment edges alone — there is no
/// explicit ordering input. Asserts the emitted order respects every relation.
/// </summary>
public class OrderingTests
{
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
