// ReSharper disable once CheckNamespace
namespace Sparkitect.DI.GeneratorAttributes;

public enum FactoryGenerationType
{
    Service,
    Factory
}

[AttributeUsage(AttributeTargets.Class)]
public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;

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

[FactoryGenerationType(FactoryGenerationType.Factory)]
public class KeyedFactoryAttribute<TBase> : Attribute, IFactoryMarker<TBase> where TBase : class
{
    [Key]
    public string? Key { get; set; }
    
    [KeyProperty]
    public string? KeyPropertyName { get; set; }
}

[FactoryGenerationType(FactoryGenerationType.Service)]
public class CreateServiceFactoryAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;

[FactoryGenerationType(FactoryGenerationType.Service)]
public class SingletonAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;

public abstract class FacadeMarkerAttribute<TFacade> : Attribute where TFacade : class;