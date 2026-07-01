using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Sub-resource ownership: when a description sub-declares a resource inside another description's
/// Declare, the transaction records the sub-chain → owning-chain edge the render graph walks to
/// attribute a sub-declared increment back to the composite root. Also pins the chain-keyed
/// same-instance invariant the moment-resolution reuse and compose-over-by-reference both rely on.
/// </summary>
public class SubResourceOwnershipTests
{
    [Test]
    public async Task NestedSubDeclaration_RecordsOwningChainForEachSubChain()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // StagingDescription sub-declares a host leaf and a device leaf inside its Declare.
        var topRef = tx.Declare(new StagingDescription(HostSize: 16, DeviceSize: 16, Count: 3));
        var compositeChain = topRef.Resource;

        foreach (var subChain in ledger.Chains.Keys)
        {
            if (subChain == compositeChain)
            {
                // The top-level composite chain has no owner.
                await Assert.That(tx.TryGetOwningChain(subChain, out _)).IsFalse();
                continue;
            }

            // Each sub-declared chain resolves to the composite's declaring chain.
            var found = tx.TryGetOwningChain(subChain, out var owningChain);
            await Assert.That(found).IsTrue();
            await Assert.That(owningChain).IsEqualTo(compositeChain);
        }
    }

    [Test]
    public async Task SameChainDifferentEpoch_ResolvesToReferenceEqualInstance()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // SelfIncrementingDescription advances its own chain one epoch: base + one increment.
        var baseRef = tx.Declare(new SelfIncrementingDescription(Size: 7));

        // A ledger-minted reference to the incremented node — same chain, different epoch.
        var incrementNode = ledger.ChainFor(baseRef.Resource)[1];
        var incrementedRef = ledger.ReferenceTo<SyntheticBuffer>(incrementNode.Id);

        var ctx = new InstanceContext(tx);
        var atBase = ctx.Resolve(baseRef);
        var atIncrement = ctx.Resolve(incrementedRef);

        // The chain-keyed cache collapses both epochs to the one instance.
        await Assert.That(atIncrement).IsSameReferenceAs(atBase);
    }
}
