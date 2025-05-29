namespace Sparkitect.DI.GeneratorAttributes;

/// <summary>
/// Marks a constructor parameter as the key for a KeyedFactory.
/// The parameter must be of type string, Identification, or OneOf&lt;Identification, string&gt;
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class KeyAttribute : Attribute;