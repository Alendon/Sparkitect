using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphing.Compile;

/// <summary>
/// Link-stage structural diagnostics, each carrying the provenance needed to explain it. The
/// compile outcome is a <c>Result&lt;CompiledPlan, CompileError&gt;</c>; these cases are populated
/// by the collect+link compile (built across later plans) and consumed as ground truth.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record CompileError
{
    /// <summary>
    /// Two increments declared from the same source epoch. Concurrent writers are structurally
    /// inexpressible; this names both produced epochs and their shared source.
    /// </summary>
    public sealed partial record Fork(
        GraphNodeId SourceEpoch,
        GraphNodeId FirstIncrement,
        GraphNodeId SecondIncrement) : CompileError;

    /// <summary>A cycle in the data-flow ordering, naming the participating nodes.</summary>
    public sealed partial record Cycle(IReadOnlyList<GraphNodeId> Participants) : CompileError;

    /// <summary>
    /// A Read of an epoch with no producing increment — unschedulable by construction (the base
    /// epoch is the canonical case). Names the read edge and the unproducible epoch.
    /// </summary>
    public sealed partial record UnproducibleRead(GraphNodeId Reader, GraphNodeId UnproducibleEpoch) : CompileError;

    /// <summary>A referenced moment with no marked increment, naming the moment and its readers.</summary>
    public sealed partial record UndefinedMoment(Identification Moment, IReadOnlyList<GraphNodeId> Readers) : CompileError;

    /// <summary>Two increments marked with the same moment, naming the moment and both provenances.</summary>
    public sealed partial record DuplicateMoment(
        Identification Moment,
        GraphNodeId FirstIncrement,
        GraphNodeId SecondIncrement) : CompileError;

    /// <summary>A description instance declared more than once, naming the reused declaration node.</summary>
    public sealed partial record DescriptionReuse(GraphNodeId ReusedDeclaration) : CompileError;
}
