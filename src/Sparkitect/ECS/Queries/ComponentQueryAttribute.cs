namespace Sparkitect.ECS.Queries;

/// <summary>
/// Strategy marker attribute indicating the source generator should process this partial class
/// as a component query, generating constructor forwarding and component ID metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ComponentQueryAttribute : Attribute;
