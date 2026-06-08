using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Modding;

namespace Sparkitect.Tests.RenderGraph;

/// <summary>
/// Pins the PostRegistry publishable-consistency decision: a Publishable resource whose bound manager does
/// not implement <c>IGraphPushTargetFor&lt;T&gt;</c> for that resource is detected as inconsistent (the gate
/// that makes PostRegistry throw), while a correctly-bound publishable manager passes. Synthetic resource
/// types carry fixed Identifications so the check is deterministic without running the registry pipeline.
/// </summary>
public class PublishableConsistencyTests
{
    private static readonly Identification ResourceIdA = Identification.Create(7, 7, 1);
    private static readonly Identification ResourceIdB = Identification.Create(7, 7, 2);

    [Test]
    public async Task PublishableResource_ManagerLacksPushTarget_IsInconsistent()
    {
        var consistent = RenderGraphManager.ManagerImplementsPushTargetFor(
            typeof(NonPushManager), ResourceIdA);

        await Assert.That(consistent).IsFalse();
    }

    [Test]
    public async Task PublishableResource_ManagerImplementsPushTarget_IsConsistent()
    {
        var consistent = RenderGraphManager.ManagerImplementsPushTargetFor(
            typeof(PushManagerForA), ResourceIdA);

        await Assert.That(consistent).IsTrue();
    }

    [Test]
    public async Task PushTarget_ForDifferentResourceId_IsNotMatched()
    {
        // The manager pushes ResourceA, so it must not satisfy the consistency check for ResourceB's id.
        var consistent = RenderGraphManager.ManagerImplementsPushTargetFor(
            typeof(PushManagerForA), ResourceIdB);

        await Assert.That(consistent).IsFalse();
    }

    private sealed class FakeResourceA : IHasIdentification
    {
        public static Identification Identification => ResourceIdA;
    }

    private sealed class NonPushManager : IGraphResourceManager;

    private sealed class PushManagerForA : IGraphPushTargetFor<FakeResourceA>
    {
        public void Publish(FakeResourceA value) { }
    }
}
