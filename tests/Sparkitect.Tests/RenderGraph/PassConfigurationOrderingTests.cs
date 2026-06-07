using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Tests.RenderGraph;

public class PassConfigurationOrderingTests
{
    private static readonly Identification PassX = Identification.Create(1, 2, 1);
    private static readonly Identification PassY = Identification.Create(1, 2, 2);
    private static readonly Identification PassP = Identification.Create(1, 2, 3);

    private sealed class TargetX : IHasIdentification
    {
        public static Identification Identification => PassX;
    }

    private sealed class TargetY : IHasIdentification
    {
        public static Identification Identification => PassY;
    }

    /// <summary>
    /// Fake builder that records every node and edge so carrier edge production can be
    /// asserted without a compiler.
    /// </summary>
    private sealed class RecordingBuilder : IExecutionGraphBuilder
    {
        public List<Identification> Nodes { get; } = [];
        public List<(Identification From, Identification To, bool Optional)> Edges { get; } = [];

        public void AddNode(Identification node) => Nodes.Add(node);

        public void AddEdge(Identification from, Identification to, bool optional)
            => Edges.Add((from, to, optional));

        public IReadOnlyList<Identification> Resolve() => Nodes;
    }

    [Test]
    public async Task ApplyEdges_OrderAfter_AddsEdgeFromTargetToOwner()
    {
        var cfg = new PassConfiguration(
            orderAfter: [new OrderAfterAttribute<TargetX>()],
            orderBefore: [])
        {
            OwnerId = PassP
        };
        var builder = new RecordingBuilder();

        cfg.ApplyEdges(builder);

        await Assert.That(builder.Nodes).Contains(PassP);
        await Assert.That(builder.Edges).HasCount().EqualTo(1);
        await Assert.That(builder.Edges[0].From).IsEqualTo(PassX);
        await Assert.That(builder.Edges[0].To).IsEqualTo(PassP);
    }

    [Test]
    public async Task ApplyEdges_OrderBefore_AddsEdgeFromOwnerToTarget()
    {
        var cfg = new PassConfiguration(
            orderAfter: [],
            orderBefore: [new OrderBeforeAttribute<TargetY>()])
        {
            OwnerId = PassP
        };
        var builder = new RecordingBuilder();

        cfg.ApplyEdges(builder);

        await Assert.That(builder.Edges).HasCount().EqualTo(1);
        await Assert.That(builder.Edges[0].From).IsEqualTo(PassP);
        await Assert.That(builder.Edges[0].To).IsEqualTo(PassY);
    }
}
