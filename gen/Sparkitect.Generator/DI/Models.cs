namespace Sparkitect.Generator.DI;

/// <summary>
/// Model for generating singleton service factories
/// </summary>
public record ServiceFactoryModel(
    string ServiceType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ImmutableValueArray<ConstructorArgument> ConstructorArguments,
    ImmutableValueArray<RequiredProperty> RequiredProperties);

/// <summary>
/// Model for generating entrypoint factories that share a common base type
/// </summary>
// Entrypoint factories removed; configuration entrypoints are discovered at runtime

/// <summary>
/// Model for generating keyed factories with string or Identification-based keys
/// </summary>
public record KeyedFactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ImmutableValueArray<ConstructorArgument> ConstructorArguments,
    ImmutableValueArray<RequiredProperty> RequiredProperties,
    KeyInfo? KeyInfo);

public record ConstructorArgument(string Type, bool IsOptional);
public record RequiredProperty(string Type, string SetterName, bool IsOptional);

/// <summary>
/// Base class for key information used in keyed factories
/// </summary>
public abstract record KeyInfo;
public record DirectKeyInfo(string KeyValue) : KeyInfo; // Direct key is always string
public record PropertyKeyInfo(string PropertyName, string ReturnType) : KeyInfo; // Property can return string, Identification, or OneOf<Identification, string>

/// <summary>
/// Model for individual singleton services that will be registered in a container
/// </summary>
public record SingletonModel(
    string FactoryFullName);     // e.g., "global::Sparkitect.Modding.RegistryManager_Factory"

/// <summary>
/// Model for generating singleton container configurators
/// </summary>
public record SingletonContainerModel(
    string ConfiguratorClassName,        // e.g., "SparkitectConfigurator", "TestModConfigurator"
    string Namespace,                   // e.g., "Sparkitect", "DiTest"
    ImmutableValueArray<SingletonModel> Singletons); // List of singleton services to register
