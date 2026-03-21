using Sparkitect.Utilities;

namespace Sparkitect.Generator.Metadata;

/// <summary>
/// Represents a constructor parameter of a metadata type.
/// The SG analyzes the metadata constructor and matches attributes from the target symbol.
/// </summary>
/// <param name="AttributeTypeName">Full type name of the attribute (non-generic base)</param>
/// <param name="IsNullable">If true, attribute is optional (? modifier)</param>
/// <param name="IsArray">If true, multiple instances allowed ([] modifier)</param>
/// <param name="Instances">Attribute instances found on the target symbol matching this parameter type</param>
public record MetadataConstructorParam(
    string AttributeTypeName,
    bool IsNullable,
    bool IsArray,
    ImmutableValueArray<MetadataAttributeInstance> Instances);

/// <summary>
/// Represents a single attribute instance applied to a target symbol.
/// Contains enough information to exactly reproduce the attribute construction.
/// </summary>
/// <param name="GenericArgs">Generic type arguments</param>
/// <param name="CtorArgs">Raw literal constructor arguments</param>
public record MetadataAttributeInstance(
    ImmutableValueArray<string> GenericArgs,
    ImmutableValueArray<string> CtorArgs);
