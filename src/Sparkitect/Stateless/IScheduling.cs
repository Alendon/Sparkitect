using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Defines a scheduling implementation for stateless functions.
/// Scheduling instances are created per-function with ordering attributes as constructor params.
/// </summary>
/// <typeparam name="TStatelessFunction">The stateless function attribute type this scheduling handles.</typeparam>
/// <typeparam name="TContext">The contextual data type for filtering/configuration.</typeparam>
/// <typeparam name="TRegistry">The registry this scheduling belongs to (derived from TStatelessFunction).</typeparam>
public interface IScheduling<TStatelessFunction, TContext, TRegistry>
    where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
    where TContext : class
    where TRegistry : IRegistry
{
    /// <summary>
    /// Builds graph nodes/edges for this function. Typically adds one node and its ordering edges.
    /// </summary>
    /// <param name="builder">Builder for constructing the DAG.</param>
    /// <param name="context">Contextual data for filtering/configuration.</param>
    /// <param name="functionId">The function's identification.</param>
    void BuildGraph(IExecutionGraphBuilder builder, TContext context, Identification functionId);
}