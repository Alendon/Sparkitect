using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Tests.Stateless;

// Golden characterization of ExecutionGraphBuilder.Resolve ordering. These pins guard the
// SC-2 "no behavior change" contract across the QuikGraph -> shared-core migration (Pitfall 1):
// the exact Resolve() sequence for a wide fan-out, a diamond, and a chain is locked here.
public class ExecutionGraphBuilderCharacterizationTests
{
    private static readonly Identification R = Identification.Create(1, 2, 10);
    private static readonly Identification A = Identification.Create(1, 2, 11);
    private static readonly Identification B = Identification.Create(1, 2, 12);
    private static readonly Identification C = Identification.Create(1, 2, 13);
    private static readonly Identification D = Identification.Create(1, 2, 14);

    // Wide fan-out: one root before four otherwise-unconstrained successors. In-degree is equal
    // across A..D, so the emitted tie order is the ordering machinery's tiebreak signature.
    [Test]
    public async Task FanOut_ResolvesToGoldenOrder()
    {
        var builder = new ExecutionGraphBuilder();
        builder.AddNode(R);
        builder.AddNode(A);
        builder.AddNode(B);
        builder.AddNode(C);
        builder.AddNode(D);
        builder.AddEdge(R, A, optional: false);
        builder.AddEdge(R, B, optional: false);
        builder.AddEdge(R, C, optional: false);
        builder.AddEdge(R, D, optional: false);

        var order = builder.Resolve();

        await Assert.That(order).IsEquivalentTo(new[] { R, A, B, C, D }, CollectionOrdering.Matching);
    }

    // Diamond: Top before both middles, both middles before Bottom.
    [Test]
    public async Task Diamond_ResolvesToGoldenOrder()
    {
        var builder = new ExecutionGraphBuilder();
        builder.AddNode(A); // Top
        builder.AddNode(B); // left middle
        builder.AddNode(C); // right middle
        builder.AddNode(D); // Bottom
        builder.AddEdge(A, B, optional: false);
        builder.AddEdge(A, C, optional: false);
        builder.AddEdge(B, D, optional: false);
        builder.AddEdge(C, D, optional: false);

        var order = builder.Resolve();

        await Assert.That(order).IsEquivalentTo(new[] { A, B, C, D }, CollectionOrdering.Matching);
    }

    // Linear chain: A before B before C.
    [Test]
    public async Task Chain_ResolvesToGoldenOrder()
    {
        var builder = new ExecutionGraphBuilder();
        builder.AddNode(A);
        builder.AddNode(B);
        builder.AddNode(C);
        builder.AddEdge(A, B, optional: false);
        builder.AddEdge(B, C, optional: false);

        var order = builder.Resolve();

        await Assert.That(order).IsEquivalentTo(new[] { A, B, C }, CollectionOrdering.Matching);
    }

    // Fail-loud (D-07 unwrap-or-throw): a required edge to an absent node throws at the SF layer,
    // naming the missing endpoint.
    [Test]
    public async Task RequiredEdge_MissingEndpoint_ThrowsNamingMissing()
    {
        var builder = new ExecutionGraphBuilder();
        builder.AddNode(A);
        builder.AddEdge(A, B, optional: false); // B never added

        await Assert.That(() => builder.Resolve())
            .Throws<InvalidOperationException>()
            .WithMessageMatching($"*Required ordering dependency not found: {B}*");
    }

    // D-04 leveled-up diagnostics: a cycle throws naming the participating nodes (the pre-migration
    // message named none).
    [Test]
    public async Task Cycle_ThrowsNamingParticipants()
    {
        var builder = new ExecutionGraphBuilder();
        builder.AddNode(A);
        builder.AddNode(B);
        builder.AddEdge(A, B, optional: false);
        builder.AddEdge(B, A, optional: false);

        await Assert.That(() => builder.Resolve())
            .Throws<InvalidOperationException>()
            .WithMessageMatching($"*Cycle detected in function ordering graph: *{A}*");
    }
}
