using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Determinism: the same set of declarations recorded into two ledgers in two different declaration
/// orders compiles to an identical plan (ordering + resolved epochs). No pass-setup order may leak
/// into the compile outcome — node identity is mint-order-dependent, so equality is asserted over the
/// stable provenance+epoch projection rather than raw minted ids.
/// </summary>
public class CompileDeterminismTests
{
    private static readonly Identification ProvA = Identification.Create(1, 1, 1);
    private static readonly Identification ProvAInc = Identification.Create(1, 2, 1);
    private static readonly Identification ProvB = Identification.Create(1, 1, 2);
    private static readonly Identification ProvBInc = Identification.Create(1, 2, 2);

    [Test]
    public async Task TwoDeclarationOrders_ProduceIdenticalPlan()
    {
        // Order 1: declare A, increment A, declare B (reads A1), increment B.
        var first = BuildOrderOne();
        // Order 2: declare B first, then A and its increment, then wire the read + B's increment.
        var second = BuildOrderTwo();

        var planOne = await Compile(first.Ledger);
        var planTwo = await Compile(second.Ledger);

        // Ordering equality over the stable (provenance, epoch-step) projection.
        var projectedOne = Project(planOne.OrderedNodes, first.Ledger);
        var projectedTwo = Project(planTwo.OrderedNodes, second.Ledger);
        await Assert.That(projectedOne).IsEquivalentTo(projectedTwo);

        // Resolved epochs equality: same chains, same positions, same epoch steps.
        var chainsOne = ProjectChains(planOne, first.Ledger);
        var chainsTwo = ProjectChains(planTwo, second.Ledger);
        await Assert.That(chainsOne).IsEquivalentTo(chainsTwo);
    }

    private static (DeclarationLedger Ledger, GraphNodeId A, GraphNodeId B) BuildOrderOne()
    {
        var ledger = new DeclarationLedger();
        var a = ledger.Declare<SyntheticBuffer>(ProvA);
        var a1 = ledger.RecordIncrement(a, ProvAInc);
        var b = ledger.Declare<SyntheticComposite>(ProvB);
        ledger.RecordRead(a1, b.Resource);
        ledger.RecordIncrement(b, ProvBInc);
        return (ledger, a.Resource, b.Resource);
    }

    private static (DeclarationLedger Ledger, GraphNodeId A, GraphNodeId B) BuildOrderTwo()
    {
        var ledger = new DeclarationLedger();
        // Reverse the declaration order: B before A.
        var b = ledger.Declare<SyntheticComposite>(ProvB);
        var a = ledger.Declare<SyntheticBuffer>(ProvA);
        var a1 = ledger.RecordIncrement(a, ProvAInc);
        ledger.RecordIncrement(b, ProvBInc);
        ledger.RecordRead(a1, b.Resource);
        return (ledger, a.Resource, b.Resource);
    }

    private static async Task<CompiledPlan> Compile(DeclarationLedger ledger)
    {
        var result = new GraphCompiler(ledger).Link();
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Ok>();
        return ((Result<CompiledPlan, CompileError>.Ok)result).Value;
    }

    // Project minted ids to (provenance, epoch-step) — stable across declaration orders.
    private static List<(Identification Provenance, int Step)> Project(
        IReadOnlyList<GraphNodeId> order,
        DeclarationLedger ledger)
    {
        var byId = ledger.Nodes.ToDictionary(n => n.Id);
        return order.Select(id => (byId[id].Provenance, byId[id].Epoch.Step)).ToList();
    }

    private static List<(Identification Provenance, int Step, int Position)> ProjectChains(
        CompiledPlan plan,
        DeclarationLedger ledger)
    {
        var byId = ledger.Nodes.ToDictionary(n => n.Id);
        return plan.ResolvedChains.Values
            .SelectMany(chain => chain)
            .Select(resolved => (byId[resolved.Node].Provenance, resolved.Epoch.Step, resolved.Position))
            .OrderBy(t => t.Item1.ModId).ThenBy(t => t.Item1.CategoryId).ThenBy(t => t.Item1.ItemId)
            .ThenBy(t => t.Step)
            .ToList();
    }
}
