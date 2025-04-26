using System.Text.Json.Serialization;
using Semver;
using Sparkitect.Utils;

namespace Sparkitect.Modding;

public record ModManifest(
    string Id,
    string Name,
    string Description,
    [property: JsonConverter(typeof(SemVersionJsonConverter))]
    SemVersion Version,
    IReadOnlyList<string> Authors,
    [property: JsonIgnore] string? ModPath,
    IReadOnlyList<ModRelationship> Relationships,
    string ModAssembly,
    IReadOnlyList<string> RequiredAssemblies);

public record struct ModRelationship(
    string Id,
    [property: JsonConverter(typeof(SemVersionRangeJsonConverter))]
    SemVersionRange VersionRange,
    ModRelationshipType RelationshipType);

public enum ModRelationshipType
{
    Dependency,
    OptionalDependency,
    Incompatible
}