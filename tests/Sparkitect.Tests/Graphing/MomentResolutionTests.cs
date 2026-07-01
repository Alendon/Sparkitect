using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Frame-time moment resolution: <see cref="IInstanceContext.ResolveMoment{T}"/> resolves a published
/// moment to the concrete instance of the resource that published it. The load-bearing proof is that a
/// moment resolves to the reference-equal producer instance — identity flows through the moment, so a
/// consumer in another pass shares the producer's chain instance rather than a distinct one.
/// </summary>
public class MomentResolutionTests
{
    private static readonly Identification TargetMoment = Identification.Create(9, 1, 1);

    [Test]
    public async Task ResolveMoment_ResolvesToTheReferenceEqualProducerInstance()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Produce marks the moment on its increment; consume references the same moment.
        var produceRef = tx.Declare(new ProduceMomentDescription(TargetMoment, Size: 16));
        tx.Declare(new ConsumeMomentDescription(TargetMoment, Size: 8));

        var plan = await AssertOk(new GraphCompiler(ledger).Link());
        var ctx = new InstanceContext(tx, plan.ResolvedMoments, ledger);

        // The moment-resolved instance IS the producer's chain instance (the chain-keyed cache).
        var byMoment = ctx.ResolveMoment<SyntheticBuffer>(TargetMoment);
        var byReference = ctx.Resolve(produceRef);

        await Assert.That(byMoment).IsSameReferenceAs(byReference);
    }

    private static async Task<CompiledPlan> AssertOk(Result<CompiledPlan, CompileError> result)
    {
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Ok>();
        return ((Result<CompiledPlan, CompileError>.Ok)result).Value;
    }
}
