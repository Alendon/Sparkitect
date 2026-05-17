using JetBrains.Annotations;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Abstract base class for component access attributes (ReadComponents, WriteComponents, ExcludeComponents).
/// Enables source generators to discover all component access declarations via a single base type check.
/// </summary>
[PublicAPI]
public abstract class ComponentAccessAttribute : Attribute;
