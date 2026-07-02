using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Self-increment: a description that increments the resource it itself resolves to advances
/// that resource's epoch chain by one, using the ordinary Increment verb (no special "advance
/// myself" verb). Asserts on the public epoch-chain state.
/// </summary>
public class SelfIncrementTests
{
    [Test]
    public async Task DescriptionIncrementsItsOwnResolvedResource_AdvancesThatChainByOne()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new SelfIncrementingDescription(64));

        // The self-incrementing description declared exactly one resource chain (the leaf it resolves
        // to) and advanced it one epoch via the same Increment mechanic.
        await Assert.That(ledger.Chains).Count().IsEqualTo(1);
        await Assert.That(ledger.Increments).Count().IsEqualTo(1);

        var chain = ledger.ChainFor(ledger.Increments[0].Resource);
        await Assert.That(chain).Count().IsEqualTo(2);
        await Assert.That(chain[0].IsBaseEpoch).IsTrue();
        await Assert.That(chain[1].IsBaseEpoch).IsFalse();
        await Assert.That(chain[1].Epoch).IsEqualTo(chain[0].Epoch.Next());
    }

    [Test]
    public async Task SelfIncrement_ResolvesToTheLeafInstanceItAdvanced()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new SelfIncrementingDescription(64));
        var ctx = new InstanceContext(tx);

        var instance = ctx.Resolve(topRef);

        await Assert.That(instance.Size).IsEqualTo(64);
    }
}
