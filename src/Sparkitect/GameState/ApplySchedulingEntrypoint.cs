using Sparkitect.DI;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// TODO: Minimize generics - implement "genericless base marker" pattern.
// SchedulingAttribute currently requires 5 generic params for CLR but SG only needs TStatelessFunction.
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
/// <typeparam name="TBuilder">The graph builder type for constructing execution graphs.</typeparam>
public abstract class ApplySchedulingEntrypoint<TStatelessFunction, TContext, TBuilder>
    : IConfigurationEntrypoint<ApplySchedulingEntrypointAttribute<TStatelessFunction>>
    where TStatelessFunction : StatelessFunctionAttribute
    where TContext : class
{
    /// <summary>
    /// Builds the execution graph by instantiating scheduling objects and invoking their BuildGraph methods.
    /// </summary>
    /// <param name="builder">The graph builder.</param>
    /// <param name="context">The scheduling context.</param>
    public abstract void BuildGraph(TBuilder builder, TContext context);
}
