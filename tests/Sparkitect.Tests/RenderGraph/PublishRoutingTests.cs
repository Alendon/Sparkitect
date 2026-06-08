using System.Runtime.CompilerServices;
using Moq;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Modding;

namespace Sparkitect.Tests.RenderGraph;

/// <summary>
/// Pins the type-routed <c>Publish&lt;T&gt;</c> door fail-fast paths: an unregistered resource id (no manager
/// binding) throws, and a resource whose registered manager does not implement
/// <c>IGraphPushTargetFor&lt;T&gt;</c> throws.
/// </summary>
public class PublishRoutingTests
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_managersByType")]
    private static extern ref Dictionary<Type, IGraphResourceManager> ManagersByType(
        Sparkitect.Graphics.RenderGraph.Runtime.RenderGraph graph);

    private static Sparkitect.Graphics.RenderGraph.Runtime.RenderGraph BuildGraph(
        Mock<IRenderGraphManager> manager,
        Dictionary<Type, IGraphResourceManager> managersByType)
    {
        var graph = (Sparkitect.Graphics.RenderGraph.Runtime.RenderGraph)
            RuntimeHelpers.GetUninitializedObject(typeof(Sparkitect.Graphics.RenderGraph.Runtime.RenderGraph));

        // The routing path only reads _renderGraphManager + _managersByType. Set both directly: the manager
        // field via reflection (private readonly), the dictionary via the UnsafeAccessor.
        typeof(Sparkitect.Graphics.RenderGraph.Runtime.RenderGraph)
            .GetField("_renderGraphManager",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(graph, manager.Object);

        ManagersByType(graph) = managersByType;
        return graph;
    }

    [Test]
    public async Task Publish_UnregisteredResource_Throws()
    {
        var manager = new Mock<IRenderGraphManager>(MockBehavior.Loose);
        Type _;
        manager.Setup(m => m.TryGetManagerType(It.IsAny<Identification>(), out _!)).Returns(false);

        IExternalResourceHandler handler = BuildGraph(manager, new Dictionary<Type, IGraphResourceManager>());

        await Assert.That(() => handler.Publish(EntityListResource.Create(ReadOnlySpan<GpuRenderEntity>.Empty)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Publish_ManagerWithoutPushTarget_Throws()
    {
        var nonPushManager = new NonPushManager();
        var managerType = nonPushManager.GetType();

        var manager = new Mock<IRenderGraphManager>(MockBehavior.Loose);
        manager.Setup(m => m.TryGetManagerType(StorageBufferView.Identification, out managerType!))
            .Returns(true);

        var managersByType = new Dictionary<Type, IGraphResourceManager> { [managerType] = nonPushManager };
        IExternalResourceHandler handler = BuildGraph(manager, managersByType);

        // StorageBufferView is a registered resource; its routed manager here does not implement
        // IGraphPushTargetFor<StorageBufferView>, so the door's cast fails fast.
        await Assert.That(() => handler.Publish(FakeStorageBufferView()))
            .Throws<InvalidOperationException>();
    }

    private static StorageBufferView FakeStorageBufferView() =>
        (StorageBufferView)RuntimeHelpers.GetUninitializedObject(typeof(StorageBufferView));

    private sealed class NonPushManager : IGraphResourceManager;
}
