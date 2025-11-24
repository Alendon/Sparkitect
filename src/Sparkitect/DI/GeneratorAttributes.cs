// ReSharper disable once CheckNamespace
namespace Sparkitect.DI.GeneratorAttributes;

/// <summary>
/// Specifies the type of factory to generate for a service or keyed factory.
/// </summary>
public enum FactoryGenerationType
{
    /// <summary>
    /// Generates a service factory for dependency injection.
    /// </summary>
    Service,

    /// <summary>
    /// Generates a keyed factory for key-based resolution.
    /// </summary>
    Factory
}

/// <summary>
/// Specifies the factory generation type for a class. Used internally by source generators.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
#pragma warning disable CS9113 // Parameter is unread (read by source generators)
public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;
#pragma warning restore CS9113

/// <summary>
/// Marker interface for factory attributes that generate service factories
/// </summary>
public interface IFactoryMarker<TExposedType> where TExposedType : class;

/// <summary>
/// Marks a property parameter (/named argument) as the key for a KeyedFactory.
/// The parameter must be of type string
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute;

/// <summary>
/// Marks a property parameter (/named argument) as containing the name of a static property that provides the key for a KeyedFactory.
/// The attribute property must have a public getter and setter (required for named arguments).
/// The referenced static property must have a public getter and return string, Identification, or OneOf&lt;Identification, string&gt;
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyPropertyAttribute : Attribute;

/// <summary>
/// Marks a class for keyed factory generation. The class will be registered in a factory container with the specified key.
/// Either <see cref="Key"/> or <see cref="KeyPropertyName"/> must be specified, but not both.
/// Keys must be of type <see cref="string"/> or <see cref="Sparkitect.Modding.Identification"/>.
/// </summary>
/// <typeparam name="TBase">The base type or interface that this factory implements.</typeparam>
/// <example>
/// <code>
/// [KeyedFactory&lt;IProcessor&gt;(Key = "json")]
/// internal class JsonProcessor : IProcessor { }
/// </code>
/// </example>
[FactoryGenerationType(FactoryGenerationType.Factory)]
public class KeyedFactoryAttribute<TBase> : Attribute, IFactoryMarker<TBase> where TBase : class
{
    /// <summary>
    /// Gets or sets the key value for this factory. Must be a string literal.
    /// Cannot be used together with <see cref="KeyPropertyName"/>.
    /// </summary>
    [Key]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the name of a static property that provides the key for this factory.
    /// The referenced property must return <see cref="string"/> or <see cref="Sparkitect.Modding.Identification"/>.
    /// Cannot be used together with <see cref="Key"/>.
    /// </summary>
    [KeyProperty]
    public string? KeyPropertyName { get; set; }
}

[FactoryGenerationType(FactoryGenerationType.Service)]
internal class CreateServiceFactoryAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;

/// <summary>
/// Base class for facade marker attributes. Facades provide subsystem-exclusive APIs that are not normally
/// resolvable through the main core container. Allows specific subsystems to access specialized interfaces
/// (e.g., registries calling manager methods) while keeping those APIs hidden from general DI resolution.
/// Triggers source generation of facade configurators.
/// </summary>
/// <typeparam name="TFacade">The exclusive facade interface type accessible only to the specific subsystem.</typeparam>
public abstract class FacadeMarkerAttribute<TFacade> : Attribute where TFacade : class;