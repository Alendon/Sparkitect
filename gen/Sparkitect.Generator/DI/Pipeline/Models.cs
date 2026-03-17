using System.Collections.Generic;

namespace Sparkitect.Generator.DI.Pipeline;

/// <summary>
/// Discriminated union for factory intent: Service (singleton) or Keyed (string key).
/// </summary>
public abstract record FactoryIntent
{
    private FactoryIntent() { }

    public sealed record Service : FactoryIntent;
    public sealed record Keyed(string Key) : FactoryIntent;
}

/// <summary>
/// Discriminated union for configurator kind: Service (core container) or Keyed (factory container).
/// </summary>
public abstract record ConfiguratorKind
{
    private ConfiguratorKind() { }

    public sealed record Service : ConfiguratorKind;
    public sealed record Keyed(string BaseType) : ConfiguratorKind;
}

/// <summary>
/// Unified factory model used by the DI pipeline for both service and keyed factories.
/// </summary>
public record FactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ImmutableValueArray<ConstructorArgument> ConstructorArguments,
    ImmutableValueArray<RequiredProperty> RequiredProperties,
    FactoryIntent Intent,
    ImmutableValueArray<string> OptionalModIds);

/// <summary>
/// Registration model for a factory in a configurator.
/// Empty ConditionalModIds means unconditional registration.
/// </summary>
public record RegistrationModel(
    string FactoryTypeName,
    ImmutableValueArray<string> ConditionalModIds);

/// <summary>
/// Combined factory and registration for pipeline boundary crossing.
/// </summary>
public record FactoryWithRegistration(
    FactoryModel Factory,
    RegistrationModel Registration);

/// <summary>
/// Options for rendering a configurator class.
/// </summary>
public record ConfiguratorOptions(
    string ClassName,
    string Namespace,
    string BaseType,
    string EntrypointAttribute,
    ConfiguratorKind Kind,
    bool IsPartial = false,
    string? MethodName = null,
    string? ModuleTypeFullName = null);

/// <summary>
/// Represents a constructor parameter dependency for factory generation.
/// </summary>
public record ConstructorArgument(string Type, bool IsOptional);

/// <summary>
/// Represents a required property dependency for factory generation.
/// </summary>
public record RequiredProperty(string Type, string SetterName, bool IsOptional, string DeclaringTypeName);

/// <summary>
/// Base interface for metadata models that know how to produce their own C# code lines
/// for inclusion in a metadata entrypoint class.
/// </summary>
public interface IMetadataModel
{
    /// <summary>
    /// Renders the C# code lines that configure resolution metadata for this entry.
    /// </summary>
    IReadOnlyList<string> RenderCodeLines();
}

/// <summary>
/// Metadata model representing a facade mapping from a dependency type to its backing service type.
/// </summary>
public record FacadeMetadataModel(string DependencyType, string FacadedType) : IMetadataModel
{
    public IReadOnlyList<string> RenderCodeLines() =>
    [
        $"dependencies.TryAdd(typeof({DependencyType}), new());",
        $"dependencies[typeof({DependencyType})].Add(new global::Sparkitect.DI.Resolution.FacadeMapping(typeof({FacadedType})));"
    ];
}
