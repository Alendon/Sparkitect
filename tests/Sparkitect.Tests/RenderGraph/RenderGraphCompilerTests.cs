using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Modding;

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
    public async Task Compile_EdgeViaOrderingAdapter_ReordersPasses()
    {
        var compiler = new RenderGraphCompiler();
        // Add copy first, compute second; the ordering edge must force compute before copy.
        compiler.AddPass(PassA, new TestPass("copy"));
        compiler.AddPass(PassB, new TestPass("compute"));

        // Apply edge (compute -> copy) through the adapter, dropping the optional flag.
        var adapter = new RenderGraphOrderingBuilder(compiler);
        adapter.AddEdge(from: PassB, to: PassA, optional: false);

        var compiled = compiler.Compile();

        await Assert.That(compiled.OrderedPasses[0].Id).IsEqualTo(PassB);
        await Assert.That(compiled.OrderedPasses[1].Id).IsEqualTo(PassA);
    }

    [Test]
    public async Task OrderingAdapter_Resolve_ThrowsNotSupported()
    {
        var compiler = new RenderGraphCompiler();
        var adapter = new RenderGraphOrderingBuilder(compiler);

        await Assert.That(() => adapter.Resolve()).Throws<NotSupportedException>();
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

}
