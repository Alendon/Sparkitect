namespace Sparkitect.DI.GeneratorAttributes;

/// <summary>
/// Marks a constructor parameter as containing the name of a static property that provides the key for a KeyedFactory.
/// The static property must return string, Identification, or OneOf&lt;Identification, string&gt;
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class KeyPropertyAttribute : Attribute;