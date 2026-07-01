using JetBrains.Annotations;
using Sparkitect.Utils.DU;

namespace Sparkitect.Graphing.Compile;

/// <summary>The outcome of a Link: either a valid <see cref="CompiledPlan"/> or the first <see cref="CompileError"/>. A thin wrapper over <see cref="Result{TOk, TError}"/> that names the two outcomes.</summary>
[PublicAPI]
public static class LinkResult
{
    /// <summary>The successful outcome: a structurally valid plan.</summary>
    public static Result<CompiledPlan, CompileError> Linked(CompiledPlan plan) => plan;

    /// <summary>The failed outcome: the first structural diagnostic, carrying its provenance.</summary>
    public static Result<CompiledPlan, CompileError> Failed(CompileError error) => error;
}
