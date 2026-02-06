using QuikGraph;
using Sparkitect.DI.Ordering;

namespace Sparkitect.Tests.DI.Ordering;

public class EntrypointOrderingResolverTests
{
    private readonly EntrypointOrderingResolver _resolver = new();

    [Test]
    public async Task NoEdges_ReturnsLexicographicOrder()
    {
        // Arrange
        var nodes = new[] { "C", "A", "B" };
        var edges = Array.Empty<Edge<string>>();

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert
        await Assert.That(result).IsEquivalentTo(new[] { "A", "B", "C" });
    }

    [Test]
    public async Task SingleNode_ReturnsSingleNode()
    {
        // Arrange
        var nodes = new[] { "A" };
        var edges = Array.Empty<Edge<string>>();

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo("A");
    }

    [Test]
    public async Task EmptyNodes_ReturnsEmpty()
    {
        // Arrange
        var nodes = Array.Empty<string>();
        var edges = Array.Empty<Edge<string>>();

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SimpleChain_RespectsEdges()
    {
        // Arrange: A -> B -> C
        var nodes = new[] { "C", "B", "A" };
        var edges = new[]
        {
            new Edge<string>("A", "B"),
            new Edge<string>("B", "C"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert
        await Assert.That(result).IsEquivalentTo(new[] { "A", "B", "C" });
    }

    [Test]
    public async Task ReverseChain_RespectsEdges()
    {
        // Arrange: C -> B -> A
        var nodes = new[] { "A", "B", "C" };
        var edges = new[]
        {
            new Edge<string>("C", "B"),
            new Edge<string>("B", "A"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert
        await Assert.That(result).IsEquivalentTo(new[] { "C", "B", "A" });
    }

    [Test]
    public async Task DiamondDependency_RespectsEdgesWithTiebreaker()
    {
        // Arrange: A -> B, A -> C, B -> D, C -> D (diamond shape)
        var nodes = new[] { "D", "C", "B", "A" };
        var edges = new[]
        {
            new Edge<string>("A", "B"),
            new Edge<string>("A", "C"),
            new Edge<string>("B", "D"),
            new Edge<string>("C", "D"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: A first, D last, B before C (lexicographic tie)
        await Assert.That(result[0]).IsEqualTo("A");
        await Assert.That(result[3]).IsEqualTo("D");
        await Assert.That(result[1]).IsEqualTo("B");
        await Assert.That(result[2]).IsEqualTo("C");
    }

    [Test]
    public async Task LexicographicTiebreaker_UsesOrdinalComparison()
    {
        // Arrange: no edges, ordinal ordering (uppercase before lowercase)
        var nodes = new[] { "beta", "Alpha", "alpha" };
        var edges = Array.Empty<Edge<string>>();

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: Ordinal comparison - uppercase letters come before lowercase
        await Assert.That(result).IsEquivalentTo(new[] { "Alpha", "alpha", "beta" });
    }

    [Test]
    public async Task CycleDetected_ThrowsInvalidOperationException()
    {
        // Arrange: A -> B -> C -> A (cycle)
        var nodes = new[] { "A", "B", "C" };
        var edges = new[]
        {
            new Edge<string>("A", "B"),
            new Edge<string>("B", "C"),
            new Edge<string>("C", "A"),
        };

        // Act & Assert
        await Assert.That(() => _resolver.Resolve(nodes, edges))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("Cycle");
    }

    [Test]
    public async Task CycleDetected_MessageListsCycleParticipants()
    {
        // Arrange: A -> B -> C -> A (cycle)
        var nodes = new[] { "A", "B", "C" };
        var edges = new[]
        {
            new Edge<string>("A", "B"),
            new Edge<string>("B", "C"),
            new Edge<string>("C", "A"),
        };

        // Act
        InvalidOperationException? caught = null;
        try
        {
            _resolver.Resolve(nodes, edges);
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        // Assert
        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).Contains("A");
        await Assert.That(caught.Message).Contains("B");
        await Assert.That(caught.Message).Contains("C");
    }

    [Test]
    public async Task OptionalEdge_MissingTargetIgnored()
    {
        // Arrange: edge A -> X where X is not in nodes
        var nodes = new[] { "B", "A" };
        var edges = new[]
        {
            new Edge<string>("A", "X"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: X is ignored, lexicographic order
        await Assert.That(result).IsEquivalentTo(new[] { "A", "B" });
    }

    [Test]
    public async Task OptionalEdge_MissingSourceIgnored()
    {
        // Arrange: edge X -> A where X is not in nodes
        var nodes = new[] { "B", "A" };
        var edges = new[]
        {
            new Edge<string>("X", "A"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: X is ignored, lexicographic order
        await Assert.That(result).IsEquivalentTo(new[] { "A", "B" });
    }

    [Test]
    public async Task ParallelBranches_LexicographicWithinWave()
    {
        // Arrange: A -> C, B -> C (A and B are independent)
        var nodes = new[] { "C", "B", "A" };
        var edges = new[]
        {
            new Edge<string>("A", "C"),
            new Edge<string>("B", "C"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: A and B before C, with lex ordering for A and B
        await Assert.That(result).IsEquivalentTo(new[] { "A", "B", "C" });
    }

    [Test]
    public async Task MultipleEdgesFromSameNode()
    {
        // Arrange: A -> B, A -> C
        var nodes = new[] { "C", "B", "A" };
        var edges = new[]
        {
            new Edge<string>("A", "B"),
            new Edge<string>("A", "C"),
        };

        // Act
        var result = _resolver.Resolve(nodes, edges);

        // Assert: A first, then B and C in lex order
        await Assert.That(result[0]).IsEqualTo("A");
        await Assert.That(result[1]).IsEqualTo("B");
        await Assert.That(result[2]).IsEqualTo("C");
    }
}
