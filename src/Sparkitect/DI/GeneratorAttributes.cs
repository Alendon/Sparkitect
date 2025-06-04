// ReSharper disable once CheckNamespace
namespace Sparkitect.DI.GeneratorAttributes;

public enum FactoryGenerationType
{
    Service,
    Factory,
    Entrypoint
}

[AttributeUsage(AttributeTargets.Class)]
public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public abstract class FactoryAttribute<TExposedType> : Attribute where TExposedType : class;

[FactoryGenerationType(FactoryGenerationType.Entrypoint)]
public class EntrypointFactoryAttribute<TBase> : FactoryAttribute<TBase> where TBase : class;

/// <summary>
/// Marks a property parameter (/named argument) as the key for a KeyedFactory.
/// The parameter must be of type string
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute;

/// <summary>
/// Marks a property parameter (/named argument) as containing the name of a static property that provides the key for a KeyedFactory.
/// The Property must have a public getter and setter
/// The static property must return string, Identification, or OneOf&lt;Identification, string&gt;
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyPropertyAttribute : Attribute;

[FactoryGenerationType(FactoryGenerationType.Factory)]
public class KeyedFactoryAttribute<TBase> : FactoryAttribute<TBase> where TBase : class
{
    [Key]
    public string? Key { get; set; }
    
    [KeyProperty]
    public string? KeyPropertyName { get; set; }
}

[FactoryGenerationType(FactoryGenerationType.Service)]
public class CreateServiceFactoryAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;

[FactoryGenerationType(FactoryGenerationType.Service)]
public class SingletonAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;