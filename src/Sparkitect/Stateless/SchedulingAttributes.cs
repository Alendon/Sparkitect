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
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class SchedulingAttribute<TScheduling, TStatelessFunction, TContext, TRegistry> : Attribute
    where TScheduling : IScheduling<TStatelessFunction, TContext, TRegistry>
    where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
    where TContext : class
    where TRegistry : IRegistry;

// ===== PerFrame Category Scheduling =====

/// <summary>
/// Default scheduling for PerFrame functions. Functions execute in dependency order every frame.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PerFrameSchedulingAttribute
    : SchedulingAttribute<PerFrameScheduling, PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>;

// ===== Transition Category Schedulings =====

/// <summary>
/// Execute once when the module/state is first created.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCreateSchedulingAttribute
    : SchedulingAttribute<OnCreateScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute once when the module/state is destroyed.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnDestroySchedulingAttribute
    : SchedulingAttribute<OnDestroyScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute when the state becomes the active leaf.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameEnterSchedulingAttribute
    : SchedulingAttribute<OnFrameEnterScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute when the state stops being the active leaf.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameExitSchedulingAttribute
    : SchedulingAttribute<OnFrameExitScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;
