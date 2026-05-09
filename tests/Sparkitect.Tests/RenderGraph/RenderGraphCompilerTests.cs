using Sparkitect.Modding;
using Sparkitect.RenderGraph;

namespace Sparkitect.Tests.RenderGraph;

public class RenderGraphCompilerTests
{
    private sealed class TestPass : IPass
    {
        public string Name { get; }
        public TestPass(string name) { Name = name; }
    }

    private static readonly Identification PassA = Identification.Create(1, 1, 1);
    private static readonly Identification PassB = Identification.Create(1, 1, 2);
    private static readonly Identification UnknownPass = Identification.Create(9, 9, 9);

    [Test]
    public async Task Compile_SinglePassNoEdges_ReturnsThatPass()
    {
        var compiler = new RenderGraphCompiler();
        var pass = new TestPass("A");
        compiler.AddPass(PassA, pass);

        var compiled = compiler.Compile();

        await Assert.That(compiled.OrderedPasses).HasCount().EqualTo(1);
        await Assert.That(compiled.OrderedPasses[0].Id).IsEqualTo(PassA);
        await Assert.That(compiled.OrderedPasses[0].Pass).IsSameReferenceAs(pass);
    }

    [Test]
    public async Task Compile_TwoPassesNoEdges_PreservesBothPassesInInsertionOrder()
    {
        // QuikGraph TopologicalSortAlgorithm tiebreak is vertex-insertion-order:
        // passes added first appear first when no edges constrain the order.
        var compiler = new RenderGraphCompiler();
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddPass(PassB, new TestPass("B"));

        var compiled = compiler.Compile();

        await Assert.That(compiled.OrderedPasses).HasCount().EqualTo(2);
        await Assert.That(compiled.OrderedPasses[0].Id).IsEqualTo(PassA);
        await Assert.That(compiled.OrderedPasses[1].Id).IsEqualTo(PassB);
    }

    [Test]
    public async Task Compile_EdgeAtoB_OrdersAFirstThenB()
    {
        var compiler = new RenderGraphCompiler();
        // Insert B first to verify edge wins over insertion order.
        compiler.AddPass(PassB, new TestPass("B"));
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddOrderingEdgeInternal(from: PassA, to: PassB);

        var compiled = compiler.Compile();

        await Assert.That(compiled.OrderedPasses[0].Id).IsEqualTo(PassA);
        await Assert.That(compiled.OrderedPasses[1].Id).IsEqualTo(PassB);
    }

    [Test]
    public async Task Compile_Cycle_ThrowsInvalidOperationContainingCycle()
    {
        var compiler = new RenderGraphCompiler();
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddPass(PassB, new TestPass("B"));
        compiler.AddOrderingEdgeInternal(from: PassA, to: PassB);
        compiler.AddOrderingEdgeInternal(from: PassB, to: PassA);

        var ex = await Assert.That(() => compiler.Compile())
            .Throws<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("cycle");
    }

    [Test]
    public async Task Compile_EdgeFromUnknownPass_ThrowsInvalidOperation()
    {
        var compiler = new RenderGraphCompiler();
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddOrderingEdgeInternal(from: UnknownPass, to: PassA);

        var ex = await Assert.That(() => compiler.Compile())
            .Throws<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("unknown pass");
    }

    [Test]
    public async Task Compile_EdgeToUnknownPass_ThrowsInvalidOperation()
    {
        var compiler = new RenderGraphCompiler();
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddOrderingEdgeInternal(from: PassA, to: UnknownPass);

        var ex = await Assert.That(() => compiler.Compile())
            .Throws<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("unknown pass");
    }

    [Test]
    public async Task Compile_NoPasses_ThrowsInvalidOperationContainingNoPasses()
    {
        var compiler = new RenderGraphCompiler();

        var ex = await Assert.That(() => compiler.Compile())
            .Throws<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("no passes");
    }

    [Test]
    public async Task Compile_EmptyEdgeListConsumed_DoesNotInterfereWithSort()
    {
        // RG-22 placeholder seam consumption — at WS the production edge list is always empty.
        // This test is the regression guard against Phase 53 changes that might break the
        // empty-list path (the only path exercised in production at Phase 49 per D-D3).
        var compiler = new RenderGraphCompiler();
        compiler.AddPass(PassA, new TestPass("A"));
        compiler.AddPass(PassB, new TestPass("B"));
        // intentionally NO AddOrderingEdgeInternal calls

        var compiled = compiler.Compile();

        await Assert.That(compiled.OrderedPasses).HasCount().EqualTo(2);
        // Order is by QuikGraph deterministic tiebreak (vertex insertion order).
        await Assert.That(compiled.OrderedPasses[0].Id).IsEqualTo(PassA);
        await Assert.That(compiled.OrderedPasses[1].Id).IsEqualTo(PassB);
    }
}
