using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Link-stage moment binding: a referenced moment binds to exactly one marked increment. Zero marked
/// increments is <see cref="CompileError.UndefinedMoment"/> (naming the moment + readers); two is
/// <see cref="CompileError.DuplicateMoment"/> (both increment provenances). Assertions are on the DU
/// case and its provenance payload — never on exception-message substrings. Marking is expressed only
/// through the description constructor <see cref="Identification"/> (no pass-level marking verb).
/// </summary>
public class MomentLinkTests
{
    private static readonly Identification TargetMoment = Identification.Create(8, 1, 1);

    [Test]
    public async Task OneMarkedIncrement_BindsTheReferencedMomentToThatIncrementResultEpoch()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Produce marks the moment on its increment; consume references the same moment.
        tx.Declare(new ProduceMomentDescription(TargetMoment, Size: 16));
        tx.Declare(new ConsumeMomentDescription(TargetMoment, Size: 8));

        var plan = await AssertOk(new GraphCompiler(ledger).Link());

        // The moment binds to the single marked increment, and the bound result epoch is the produced
        // (post-increment) epoch — the consume reference resolves to exactly that.
        await Assert.That(plan.ResolvedMoments).ContainsKey(TargetMoment);
        var bound = plan.ResolvedMoments[TargetMoment];
        await Assert.That(bound.IncrementNode).IsNotEqualTo(GraphNodeId.None);
        await Assert.That(bound.ResultEpoch.IsBase).IsFalse();

        // The bound increment node is exactly the marked node in the ledger.
        var markedNode = ledger.Nodes.Single(node => node.IsMarked);
        await Assert.That(bound.IncrementNode).IsEqualTo(markedNode.Id);
        await Assert.That(bound.ResultEpoch).IsEqualTo(markedNode.Epoch);
    }

    [Test]
    public async Task ZeroMarkedIncrements_IsUndefinedMomentNamingTheReaders()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Consume references the moment, but no producer ever marks it.
        var consumeRef = tx.Declare(new ConsumeMomentDescription(TargetMoment, Size: 8));

        var undefined = await AssertError<CompileError.UndefinedMoment>(new GraphCompiler(ledger).Link());

        await Assert.That(undefined.Moment).IsEqualTo(TargetMoment);
        await Assert.That(undefined.Readers).Contains(consumeRef.Resource);
    }

    [Test]
    public async Task TwoMarkedIncrements_IsDuplicateMomentCarryingBothProvenances()
    {
        var ledger = new DeclarationLedger();
        var tx = new ResourceTransaction(ledger);

        // Two producers mark the SAME moment, and a consumer references it.
        tx.Declare(new ProduceMomentDescription(TargetMoment, Size: 16));
        tx.Declare(new ProduceMomentDescription(TargetMoment, Size: 32));
        tx.Declare(new ConsumeMomentDescription(TargetMoment, Size: 8));

        var duplicate = await AssertError<CompileError.DuplicateMoment>(new GraphCompiler(ledger).Link());

        await Assert.That(duplicate.Moment).IsEqualTo(TargetMoment);
        await Assert.That(duplicate.FirstIncrement).IsNotEqualTo(duplicate.SecondIncrement);
        await Assert.That(duplicate.FirstIncrement).IsNotEqualTo(GraphNodeId.None);
        await Assert.That(duplicate.SecondIncrement).IsNotEqualTo(GraphNodeId.None);
    }

    private static async Task<CompiledPlan> AssertOk(Result<CompiledPlan, CompileError> result)
    {
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Ok>();
        return ((Result<CompiledPlan, CompileError>.Ok)result).Value;
    }

    private static async Task<TError> AssertError<TError>(Result<CompiledPlan, CompileError> result)
        where TError : CompileError
    {
        await Assert.That(result).IsTypeOf<Result<CompiledPlan, CompileError>.Error>();
        var error = ((Result<CompiledPlan, CompileError>.Error)result).Value;
        await Assert.That(error).IsTypeOf<TError>();
        return (TError)error;
    }
}
