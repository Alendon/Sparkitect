using Sparkitect.Utilities;

namespace Sparkitect.Generator.ECS;

/// <summary>
/// Represents one component type extracted from a component access attribute's type arguments.
/// </summary>
/// <param name="FullyQualifiedName">The <c>global::Namespace.Type</c> format for code generation.</param>
/// <param name="ShortName">The type name only (e.g., "Position") for accessor method naming.</param>
public record ComponentInfo(string FullyQualifiedName, string ShortName);

/// <summary>
/// Complete model for one <c>[ComponentQuery]</c> partial class, carrying all information
/// needed by the Liquid template to generate the query implementation.
/// All collection fields use <see cref="ImmutableValueArray{T}"/> for incremental generator caching correctness.
/// </summary>
/// <param name="Namespace">The user's declaring namespace (NOT ComputeOutputNamespace -- partial class must match user namespace).</param>
/// <param name="ClassName">The name of the user's partial class.</param>
/// <param name="ReadComponents">Components declared via <c>[ReadComponents]</c> attributes, merged in declaration order.</param>
/// <param name="WriteComponents">Components declared via <c>[WriteComponents]</c> attributes, merged in declaration order.</param>
/// <param name="ExcludeComponents">Components declared via <c>[ExcludeComponents]</c> attributes, merged in declaration order.</param>
/// <param name="IsKeyed">Whether the query has an <c>[ExposeKey]</c> attribute.</param>
/// <param name="KeyTypeFullyQualified">Fully qualified key type (e.g., <c>global::Sparkitect.ECS.EntityId</c>), null when not keyed.</param>
/// <param name="KeyTypeShort">Short key type name (e.g., "EntityId"), null when not keyed.</param>
/// <param name="KeyRequired">Whether keyed iteration is required (<c>true</c>) or opportunistic (<c>false</c>).</param>
public record EcsQueryModel(
    string Namespace,
    string ClassName,
    ImmutableValueArray<ComponentInfo> ReadComponents,
    ImmutableValueArray<ComponentInfo> WriteComponents,
    ImmutableValueArray<ComponentInfo> ExcludeComponents,
    bool IsKeyed,
    string? KeyTypeFullyQualified,
    string? KeyTypeShort,
    bool KeyRequired);
