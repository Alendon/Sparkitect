using System;
using System.Linq;

namespace Sparkitect.Generator.DI;

public record SingletonModel(
    string ServiceType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties);

public record EntrypointFactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties);

public record KeyedFactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ValueCompareList<ConstructorArgument> ConstructorArguments,
    ValueCompareList<RequiredProperty> RequiredProperties,
    KeyInfo? KeyInfo);

public record ConstructorArgument(string Type, bool IsOptional);
public record RequiredProperty(string Type, string SetterName, bool IsOptional);

public abstract record KeyInfo;
public record DirectKeyInfo(string KeyValue) : KeyInfo; // Direct key is always string
public record PropertyKeyInfo(string PropertyName, string ReturnType) : KeyInfo; // Property can return string, Identification, or OneOf<Identification, string>