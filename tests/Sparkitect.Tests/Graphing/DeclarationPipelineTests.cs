using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// The description → facts → instance pipeline with sub-declaration. A composite description that
/// sub-declares a host leaf + a device leaf and increments the device produces immutable facts
/// holding the refs; building the instance via <c>CreateInstance</c> composes the POCO from the
/// dependency-first resolved sub-instances. Asserts on the public ledger query surface and the facts
/// structure, never on message substrings.
/// </summary>
public class DeclarationPipelineTests
{
    [Test]
    public async Task Composite_SubDeclaresTwoLeavesAndIncrementsDevice_ProducesImmutableFacts()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new StagingDescription(HostSize: 16, DeviceSize: 16, Count: 3));

        // Sub-declaration minted three resource chains (composite + host leaf + device leaf), and the
        // device leaf was advanced one epoch by the increment.
        await Assert.That(ledger.Chains).HasCount().EqualTo(3);
        await Assert.That(ledger.Increments).HasCount().EqualTo(1);

        // Exactly one chain grew past its base epoch (the staged device leaf).
        var grownChains = ledger.Chains.Values.Count(chain => chain.Count > 1);
        await Assert.That(grownChains).IsEqualTo(1);

        // The facts are immutable and hold the host/device refs and the CPU-side count.
        var facts = tx.FactsFor(topRef) as StagingFact;
        await Assert.That(facts).IsNotNull();
        await Assert.That(facts!.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Composite_CreateInstance_ComposesFromDependencyFirstResolvedSubInstances()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        var topRef = tx.Declare(new StagingDescription(HostSize: 8, DeviceSize: 32, Count: 5));
        var ctx = new InstanceContext(tx);

        var instance = ctx.Resolve(topRef);

        await Assert.That(instance.Host.Size).IsEqualTo(8);
        await Assert.That(instance.Device.Size).IsEqualTo(32);
        await Assert.That(instance.Count).IsEqualTo(5);
    }
}
