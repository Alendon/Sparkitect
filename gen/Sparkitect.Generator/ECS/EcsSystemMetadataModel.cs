using Sparkitect.Utilities;

namespace Sparkitect.Generator.ECS;

/// <summary>
/// Represents one query-typed parameter extracted from an ECS system method.
/// </summary>
/// <param name="QueryTypeFullyQualified">The fully qualified name of the query type
/// (e.g., <c>global::SpaceInvadersMod.MovementQuery</c>).</param>
public record QueryParameterInfo(string QueryTypeFullyQualified);

/// <summary>
/// Model carrying extraction results for one ECS system method from the metadata pipeline.
/// Contains the wrapper type information derived from the SF attribute identifier and
/// the list of query-typed parameters found on the method.
/// </summary>
/// <param name="WrapperFullTypeName">The derived wrapper type name without <c>global::</c> prefix
/// (e.g., <c>SpaceInvadersMod.GameplayGroup.MovementFunc</c>).</param>
/// <param name="WrapperTypeNamespace">The namespace portion of the wrapper type name
/// (e.g., <c>SpaceInvadersMod.GameplayGroup</c>).</param>
/// <param name="QueryParameters">The query-typed parameters extracted from the method signature.</param>
public record EcsSystemMetadataModel(
    string WrapperFullTypeName,
    string WrapperTypeNamespace,
    ImmutableValueArray<QueryParameterInfo> QueryParameters);
