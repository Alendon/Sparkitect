using Sparkitect.DI.Ordering;

namespace Sparkitect.Tests.DI.Ordering;

public class EntrypointOrderingBuilderTests
{
    [Test]
    public async Task SetCurrentType_UpdatesCurrentTypeName()
    {
        // Arrange
        var builder = new EntrypointOrderingBuilder();

        // Act
        builder.SetCurrentType("A");

        // Assert
        await Assert.That(builder.CurrentTypeName).IsEqualTo("A");
    }

    [Test]
    public async Task AddEdge_CollectsEdge()
    {
        // Arrange
        var builder = new EntrypointOrderingBuilder();

        // Act
        builder.AddEdge("A", "B");

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(1);
        var edge = builder.Edges.First();
        await Assert.That(edge.Source).IsEqualTo("A");
        await Assert.That(edge.Target).IsEqualTo("B");
    }

    [Test]
    public async Task MultipleEdges_AllCollected()
    {
        // Arrange
        var builder = new EntrypointOrderingBuilder();

        // Act
        builder.AddEdge("A", "B");
        builder.AddEdge("B", "C");
        builder.AddEdge("C", "D");

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(3);
    }

    [Test]
    public async Task SetCurrentType_CanChangeMultipleTimes()
    {
        // Arrange
        var builder = new EntrypointOrderingBuilder();

        // Act
        builder.SetCurrentType("A");
        builder.AddEdge("A", "B");
        builder.SetCurrentType("B");
        builder.AddEdge("B", "C");

        // Assert
        await Assert.That(builder.CurrentTypeName).IsEqualTo("B");
        await Assert.That(builder.Edges.Count).IsEqualTo(2);
        var edgeList = builder.Edges.ToList();
        await Assert.That(edgeList[0].Source).IsEqualTo("A");
        await Assert.That(edgeList[0].Target).IsEqualTo("B");
        await Assert.That(edgeList[1].Source).IsEqualTo("B");
        await Assert.That(edgeList[1].Target).IsEqualTo("C");
    }

    [Test]
    public async Task Edges_ReturnsReadOnlyCollection()
    {
        // Arrange
        var builder = new EntrypointOrderingBuilder();

        // Act & Assert
        await Assert.That(builder.Edges).IsTypeOf<IReadOnlyCollection<QuikGraph.Edge<string>>>();
    }
}
