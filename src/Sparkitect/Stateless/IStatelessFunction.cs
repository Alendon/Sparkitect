using System.Diagnostics;
using Sparkitect.DI.Container;
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
    /// Initializes the state function wrapper with DI container and facade mappings.
    /// </summary>
    /// <param name="container">The DI container for resolving dependencies.</param>
    /// <param name="facadeMap">Type substitution map for facade resolution.</param>
    public void Initialize(ICoreContainer container, IReadOnlyDictionary<Type, Type> facadeMap);
    
    public Identification Identification { get; }
    public Identification ParentIdentification { get; }
}