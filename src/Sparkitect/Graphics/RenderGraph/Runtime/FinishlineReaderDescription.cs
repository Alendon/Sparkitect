using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// The render-graph-owned consumer of the finishline moment, giving it a bound consumer at link: a
/// zero-mark situation surfaces as <see cref="Sparkitect.Graphing.Compile.CompileError.UndefinedMoment"/>
/// naming this reader. It carries no GPU work and resolves to no instance.
/// </summary>
[PublicAPI]
public sealed record FinishlineReaderDescription : IResourceDescription<FinishlineReader>
{
    /// <inheritdoc/>
    public DeclaredFact<FinishlineReader> Declare(IResourceTransaction tx)
    {
        tx.ReferenceMoment(GraphMomentID.Sparkitect.Finishline);
        return new FinishlineReaderFact();
    }
}

/// <summary>The empty sentinel the finishline reader resolves to; never fetched or transitioned.</summary>
[PublicAPI]
public sealed class FinishlineReader;

/// <summary>The fact for the finishline reader; composes the empty sentinel, holding no sub-references.</summary>
[PublicAPI]
public sealed record FinishlineReaderFact : DeclaredFact<FinishlineReader>
{
    /// <inheritdoc/>
    public FinishlineReader CreateInstance(IInstanceContext ctx) => new();

    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
