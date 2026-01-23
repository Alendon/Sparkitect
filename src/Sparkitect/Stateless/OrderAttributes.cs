using Sparkitect.Modding;

namespace Sparkitect.Stateless;

// TODO: SG Analyzer - Validate SchedulingParameterAttribute usage:
//   - Must only be applied to methods with a StatelessFunctionAttribute
//   - Must be combined with a matching scheduling attribute

/// <summary>
/// Marker base for scheduling parameter attributes. Used by analyzers to validate
/// that these attributes are only applied to stateless function methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public abstract class SchedulingParameterAttribute : Attribute;

/// <summary>
/// Specifies this function should execute before another function in the same scope.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeAttribute(string targetIdentifier) : SchedulingParameterAttribute
{
    public string TargetIdentifier { get; } = targetIdentifier;
}

/// <summary>
/// Specifies this function should execute after another function in the same scope.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterAttribute(string targetIdentifier) : SchedulingParameterAttribute
{
    public string TargetIdentifier { get; } = targetIdentifier;
}

/// <summary>
/// Specifies this function should execute before a function in another module/state.
/// </summary>
/// <typeparam name="TOwner">The module or state type containing the target function.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeAttribute<TOwner>(string targetIdentifier) : SchedulingParameterAttribute
    where TOwner : IHasIdentification
{
    public string TargetIdentifier { get; } = targetIdentifier;
}

/// <summary>
/// Specifies this function should execute after a function in another module/state.
/// </summary>
/// <typeparam name="TOwner">The module or state type containing the target function.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterAttribute<TOwner>(string targetIdentifier) : SchedulingParameterAttribute
    where TOwner : IHasIdentification
{
    public string TargetIdentifier { get; } = targetIdentifier;
}
