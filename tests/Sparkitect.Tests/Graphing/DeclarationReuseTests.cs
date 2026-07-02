using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// The one-declaration-per-instance rule: declaring the SAME description instance twice is rejected
/// with the <see cref="CompileError.DescriptionReuse"/> case. Asserts on the DU case, never on a
/// message substring; a fresh instance per declaration is accepted.
/// </summary>
public class DeclarationReuseTests
{
    [Test]
    public async Task DeclaringSameInstanceTwice_RejectsWithDescriptionReuseCase()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);
        var description = new LeafBufferDescription(16);

        tx.Declare(description);

        CompileError? rejection = null;
        try
        {
            tx.Declare(description);
        }
        catch (DescriptionReuseException ex)
        {
            rejection = ex.Error;
        }

        await Assert.That(rejection).IsNotNull();
        await Assert.That(rejection).IsTypeOf<CompileError.DescriptionReuse>();
    }

    [Test]
    public async Task DeclaringFreshInstancesOfSameType_IsAccepted()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        tx.Declare(new LeafBufferDescription(16));
        tx.Declare(new LeafBufferDescription(16));

        // Two distinct instances → two distinct resource chains, no rejection.
        await Assert.That(ledger.Chains).Count().IsEqualTo(2);
    }
}
