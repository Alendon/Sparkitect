using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

// TODO: Minimize generics - implement "genericless base marker" pattern.
// SchedulingAttribute currently requires 4 generic params for CLR but SG only needs TStatelessFunction.
// Consider non-generic marker base that SG can detect, with generics only for compile-time safety.

/// <summary>
/// Attribute marking an apply scheduling entrypoint for discovery.
/// </summary>
/// <typeparam name="TStatelessFunction">The stateless function attribute type this entrypoint handles.</typeparam>
public class ApplySchedulingEntrypointAttribute<TStatelessFunction> : Attribute
    where TStatelessFunction : Attribute;

/// <summary>
/// Base class for apply scheduling entrypoints. Source generators create implementations
/// that instantiate scheduling objects per-function and invoke BuildGraph.
/// Entrypoints can also be manually written by mod developers for customization.
/// </summary>
/// <typeparam name="TStatelessFunction">The stateless function attribute type.</typeparam>
/// <typeparam name="TContext">The context type.</typeparam>
/// <typeparam name="TRegistry">The registry type.</typeparam>
public abstract class ApplySchedulingEntrypoint<TStatelessFunction, TContext, TRegistry>
    : IConfigurationEntrypoint<ApplySchedulingEntrypointAttribute<TStatelessFunction>>
    where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
    where TContext : class
    where TRegistry : IRegistry
{
    /// <summary>
    /// Builds the execution graph by instantiating scheduling objects and invoking their BuildGraph methods.
    /// </summary>
    /// <param name="builder">The execution graph builder.</param>
    /// <param name="context">The scheduling context.</param>
    public abstract void BuildGraph(IExecutionGraphBuilder builder, TContext context);
}