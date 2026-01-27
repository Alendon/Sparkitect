namespace Sparkitect.Utils;

/// <summary>
/// Marks a CallerContext parameter for automatic injection by CallerContextGenerator.
/// The generator intercepts calls to methods with this attribute and injects the call site location.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InjectCallerContextAttribute : Attribute;
