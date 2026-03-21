using Sparkitect.Metadata;

namespace Sparkitect.Stateless;

/// <summary>
/// Base class for scheduling attributes. Associates a stateless function
/// with a specific scheduling metadata type via MetadataAttribute inheritance.
/// </summary>
/// <typeparam name="TScheduling">The scheduling implementation type.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class SchedulingAttribute<TScheduling> : MetadataAttribute<TScheduling>
    where TScheduling : IScheduling;
