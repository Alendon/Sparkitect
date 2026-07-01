using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// The render-graph-owned consumer of the finishline moment. The graph declares this reader into the
/// same setup transaction so the finishline has a bound consumer: at link the reference resolves to the
/// single marked increment, and a zero-mark situation surfaces as
/// <see cref="Sparkitect.Graphing.Compile.CompileError.UndefinedMoment"/> naming this present reader.
/// It carries no GPU work and resolves to no instance — it exists purely to reference the moment.
/// </summary>
[PublicAPI]
public sealed record FinishlineReaderDescription : IResourceDescription<FinishlineReader>
{
    /// <inheritdoc/>
    public DeclaredFact<FinishlineReader> Declare(IResourceTransaction tx)
    {
        // Reference the finishline moment so the present consumer is bound at link (and a zero-mark
        // surfaces UndefinedMoment naming this reader). Marking rides the producing pass's increment.
        tx.ReferenceMoment(GraphMomentID.Sparkitect.Finishline);
        return new FinishlineReaderFact();
    }
}

/// <summary>
/// The marker resource the finishline reader resolves to. It is never fetched or transitioned — the
/// reader exists only to reference the finishline moment, so the instance is an empty sentinel.
/// </summary>
[PublicAPI]
public sealed class FinishlineReader;

/// <summary>The facts for the finishline reader: it composes the empty sentinel, holding no sub-references.</summary>
[PublicAPI]
public sealed record FinishlineReaderFact : DeclaredFact<FinishlineReader>
{
    /// <inheritdoc/>
    public FinishlineReader CreateInstance(IInstanceContext ctx) => new();

    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
