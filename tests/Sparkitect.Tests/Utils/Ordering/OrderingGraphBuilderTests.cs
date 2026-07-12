using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;
using OrderResult = Sparkitect.Utils.DU.Result<
    System.Collections.Generic.IReadOnlyList<string>,
    Sparkitect.Utils.Ordering.OrderingError<string>>;

namespace Sparkitect.Tests.Utils.Ordering;

/// <summary>
/// The single home for shared ordering sort mechanics: correctness under both tiebreak
/// strategies, cycle/missing-dependency diagnostics, and edge policy (self-edge skip, parallel dedup).
/// Consumers of the core do NOT re-test these mechanics.
/// </summary>
public class OrderingGraphBuilderTests
{
    private static IReadOnlyList<string> Ok(OrderResult result) =>
        result is OrderResult.Ok ok
            ? ok.Value
            : throw new InvalidOperationException($"Expected Ok, got {result}");

    private static OrderingError<string> Err(OrderResult result) =>
        result is OrderResult.Error error
            ? error.Value
            : throw new InvalidOperationException($"Expected Error, got {result}");

    [Test]
    public async Task EmptyGraph_ReturnsOkEmptyList()
    {
        var builder = new OrderingGraphBuilder<string>();

        var result = builder.Sort(OrderingTiebreak<string>.InsertionOrder);

        await Assert.That(Ok(result)).IsEmpty();
    }

    [Test]
    public async Task SingleNode_ReturnsOkSingleNode()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");

        var result = builder.Sort(OrderingTiebreak<string>.InsertionOrder);

        await Assert.That(Ok(result)).IsEquivalentTo(new[] { "A" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Chain_AddedOutOfOrder_OrdersTopologically_Insertion()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("C");
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddEdge("A", "B", optional: false);
        builder.AddEdge("B", "C", optional: false);

        var result = builder.Sort(OrderingTiebreak<string>.InsertionOrder);

        await Assert.That(Ok(result)).IsEquivalentTo(new[] { "A", "B", "C" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Chain_AddedOutOfOrder_OrdersTopologically_Lexicographic()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("C");
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddEdge("A", "B", optional: false);
        builder.AddEdge("B", "C", optional: false);

        var result = builder.Sort(OrderingTiebreak<string>.Lexicographic(StringComparer.Ordinal));

        await Assert.That(Ok(result)).IsEquivalentTo(new[] { "A", "B", "C" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Diamond_Lexicographic_AFirstDLast_BBeforeC()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("D");
        builder.AddNode("C");
        builder.AddNode("B");
        builder.AddNode("A");
        builder.AddEdge("A", "B", optional: false);
        builder.AddEdge("A", "C", optional: false);
        builder.AddEdge("B", "D", optional: false);
        builder.AddEdge("C", "D", optional: false);

        var ordered = Ok(builder.Sort(OrderingTiebreak<string>.Lexicographic(StringComparer.Ordinal)));

        await Assert.That(ordered[0]).IsEqualTo("A");
        await Assert.That(ordered[3]).IsEqualTo("D");
        await Assert.That(ordered[1]).IsEqualTo("B");
        await Assert.That(ordered[2]).IsEqualTo("C");
    }

    [Test]
    public async Task Lexicographic_UsesOrdinalComparison()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("beta");
        builder.AddNode("Alpha");
        builder.AddNode("alpha");

        var result = builder.Sort(OrderingTiebreak<string>.Lexicographic(StringComparer.Ordinal));

        await Assert.That(Ok(result)).IsEquivalentTo(new[] { "Alpha", "alpha", "beta" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task InsertionOrder_PreservesAddOrder_NotLexicographic()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("C");
        builder.AddNode("B");
        builder.AddNode("A");

        var result = builder.Sort(OrderingTiebreak<string>.InsertionOrder);

        await Assert.That(Ok(result)).IsEquivalentTo(new[] { "C", "B", "A" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Cycle_ReturnsCycleError_NamingParticipants_DoesNotThrow()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddNode("C");
        builder.AddEdge("A", "B", optional: false);
        builder.AddEdge("B", "C", optional: false);
        builder.AddEdge("C", "A", optional: false);

        var error = Err(builder.Sort(OrderingTiebreak<string>.InsertionOrder));

        await Assert.That(error).IsTypeOf<OrderingError<string>.Cycle>();
        var participants = ((OrderingError<string>.Cycle)error).Participants;
        await Assert.That(participants).Contains("A");
        await Assert.That(participants).Contains("B");
        await Assert.That(participants).Contains("C");
    }

    [Test]
    public async Task RequiredEdge_MissingTarget_ReturnsMissingRequiredDependency_DoesNotThrow()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");
        builder.AddEdge("A", "X", optional: false);

        var error = Err(builder.Sort(OrderingTiebreak<string>.InsertionOrder));

        await Assert.That(error).IsTypeOf<OrderingError<string>.MissingRequiredDependency>();
        var missing = (OrderingError<string>.MissingRequiredDependency)error;
        await Assert.That(missing.From).IsEqualTo("A");
        await Assert.That(missing.To).IsEqualTo("X");
    }

    [Test]
    public async Task OptionalEdge_MissingTarget_DroppedSilently()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddEdge("A", "X", optional: true);

        var ordered = Ok(builder.Sort(OrderingTiebreak<string>.InsertionOrder));

        await Assert.That(ordered).DoesNotContain("X");
        await Assert.That(ordered).IsEquivalentTo(new[] { "A", "B" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task SelfEdge_Skipped_NoFalseCycle()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");
        builder.AddEdge("A", "A", optional: false);

        var ordered = Ok(builder.Sort(OrderingTiebreak<string>.InsertionOrder));

        await Assert.That(ordered).IsEquivalentTo(new[] { "A" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task ParallelDuplicateEdge_Deduped_NoDeadlockOrFalseCycle()
    {
        var builder = new OrderingGraphBuilder<string>();
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddEdge("A", "B", optional: false);
        builder.AddEdge("A", "B", optional: false);

        var ordered = Ok(builder.Sort(OrderingTiebreak<string>.InsertionOrder));

        await Assert.That(ordered).IsEquivalentTo(new[] { "A", "B" }, CollectionOrdering.Matching);
    }
}
