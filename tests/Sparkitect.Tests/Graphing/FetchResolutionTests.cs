using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Fetch resolution: the handle returned by <c>use</c> resolves via <see cref="IGraphResource{T}.Fetch"/>
/// to exactly the instance the facts' <c>CreateInstance</c> built for the frame (N=1, deps resolved
/// first). Asserts reference-equality between the handle's resolution and the context's resolution.
/// </summary>
public class FetchResolutionTests
{
    [Test]
    public async Task Fetch_ReturnsTheSameInstanceCreateInstanceBuilt()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new StagingDescription(HostSize: 8, DeviceSize: 32, Count: 5));
        var ctx = new InstanceContext(tx);
        var handle = new GraphResourceHandle<SyntheticComposite>(topRef, ctx);

        // The instance CreateInstance built for the frame (resolved once, cached at N=1).
        var built = ctx.Resolve(topRef);
        var fetched = handle.Fetch();

        await Assert.That(fetched).IsSameReferenceAs(built);
    }

    [Test]
    public async Task Fetch_IsStableAcrossCalls_SingleInFlightFrame()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new StagingDescription(HostSize: 8, DeviceSize: 32, Count: 5));
        var ctx = new InstanceContext(tx);
        var handle = new GraphResourceHandle<SyntheticComposite>(topRef, ctx);

        var first = handle.Fetch();
        var second = handle.Fetch();

        // N=1: the same in-flight frame yields the same instance, and deps resolve to one instance too.
        await Assert.That(first).IsSameReferenceAs(second);
        await Assert.That(first.Host).IsSameReferenceAs(second.Host);
    }
}
