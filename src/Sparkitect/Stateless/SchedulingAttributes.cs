using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base class for scheduling attributes. Associates a stateless function with
/// a specific scheduling implementation type.
/// </summary>
/// <typeparam name="TScheduling">The scheduling implementation type.</typeparam>
/// <typeparam name="TStatelessFunction">The stateless function attribute type.</typeparam>
/// <typeparam name="TContext">The context type (must match function's context).</typeparam>
/// <typeparam name="TRegistry">The registry type (must match function's registry).</typeparam>
/// <typeparam name="TBuilder">The graph builder type for constructing execution graphs.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class SchedulingAttribute<TScheduling, TStatelessFunction, TContext, TRegistry, TBuilder> : Attribute
    where TScheduling : IScheduling<TStatelessFunction, TContext, TRegistry, TBuilder>
    where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
    where TContext : class
    where TRegistry : IRegistry;