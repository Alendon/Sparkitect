using System.Diagnostics;
using Sparkitect.DI.Resolution;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Internal interface for source-generated stateless function wrappers.
/// </summary>
public interface IStatelessFunction
{
    /// <summary>
    /// Executes the state function.
    /// </summary>
    [DebuggerStepThrough]
    public void Execute();

    /// <summary>
    /// Initializes the state function wrapper with a resolution scope for dependency resolution.
    /// </summary>
    /// <param name="scope">The resolution scope for resolving dependencies.</param>
    public void Initialize(IResolutionScope scope);

    public Identification Identification { get; }
    public Identification ParentIdentification { get; }
}
