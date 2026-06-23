using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// The three structural link diagnostics, asserted on the <see cref="CompileError"/> DU case AND its
/// provenance payload — never on exception-message substrings.
/// </summary>
public class CompileDiagnosticTests
{
    [Test]
    public async Task Fork_TwoIncrementsOffOneSourceEpoch_IsForkCarryingBothIncrements()
    {
        var ledger = CompileFixtures.Fork(out var sharedSource);

        var result = new GraphCompiler(ledger).Link();

        var fork = await AssertError<CompileError.Fork>(result);
        await Assert.That(fork.SourceEpoch).IsEqualTo(sharedSource);
        // Both conflicting increments are named, and they are distinct.
        await Assert.That(fork.FirstIncrement).IsNotEqualTo(fork.SecondIncrement);
        await Assert.That(fork.FirstIncrement).IsNotEqualTo(GraphNodeId.None);
        await Assert.That(fork.SecondIncrement).IsNotEqualTo(GraphNodeId.None);
    }

    [Test]
    public async Task Cycle_DerivedEdgesFormALoop_IsCycleCarryingParticipants()
    {
        var ledger = CompileFixtures.Cycle();

        var result = new GraphCompiler(ledger).Link();

        var cycle = await AssertError<CompileError.Cycle>(result);
        // Every node in this fixture participates in the single loop.
        await Assert.That(cycle.Participants).HasCount().EqualTo(ledger.Nodes.Count);
    }

    [Test]
    public async Task Unproducible_ReadOfBaseEpoch_IsUnproducibleReadCarryingTheEpoch()
    {
        var ledger = CompileFixtures.UnproducibleBaseRead(out var reader, out var baseEpoch);

        var result = new GraphCompiler(ledger).Link();

        var unproducible = await AssertError<CompileError.UnproducibleRead>(result);
        await Assert.That(unproducible.Reader).IsEqualTo(reader);
        await Assert.That(unproducible.UnproducibleEpoch).IsEqualTo(baseEpoch);
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
