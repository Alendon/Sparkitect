using Sparkitect.Utilities;

namespace Sparkitect.Generator.Metadata;

/// <summary>
/// Model for a type that needs a metadata entrypoint generated.
/// One model per IHasIdentification type per metadata category.
/// </summary>
public record MetadataTargetModel(
    string TypeFullName,
    string TypeShortName,
    string TypeNamespace,
    string MetadataTypeName,
    ImmutableValueArray<MetadataConstructorParam> ConstructorParams);
