using System;
using System.Linq;

namespace Sparkitect.Generator.DI;

/// <summary>
/// Model for generating singleton service factories
/// </summary>
public record ServiceFactoryModel(
    string ServiceType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties);

/// <summary>
/// Model for generating entrypoint factories that share a common base type
/// </summary>
public record EntrypointFactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties);

/// <summary>
/// Model for generating keyed factories with string or Identification-based keys
/// </summary>
public record KeyedFactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties,
    KeyInfo? KeyInfo);

public record ConstructorArgument(string Type, bool IsOptional);
public record RequiredProperty(string Type, string SetterName, bool IsOptional);

/// <summary>
/// Base class for key information used in keyed factories
/// </summary>
public abstract record KeyInfo;
public record DirectKeyInfo(string KeyValue) : KeyInfo; // Direct key is always string
public record PropertyKeyInfo(string PropertyName, string ReturnType) : KeyInfo; // Property can return string, Identification, or OneOf<Identification, string>

public record SingletonModel;