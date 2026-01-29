using System.Text.Json.Serialization;
using Semver;
using Sparkitect.Sdk.TaskImpl.Utils;

namespace Sparkitect.Sdk.TaskImpl.Models;

/// <summary>
/// Represents a mod manifest containing metadata about a mod
/// </summary>
public record ModManifestModel(
    string Id,
    string Name,
    string Description,
    [property: JsonConverter(typeof(SemVersionJsonConverter))]
    SemVersion Version,
    IReadOnlyList<string> Authors,
    IReadOnlyList<ModRelationshipModel> Relationships,
    string ModAssembly,
    IReadOnlyList<string> RequiredAssemblies = null,
    bool IsRootMod = false);

/// <summary>
/// Represents a relationship between mods (dependency, incompatibility, etc.)
/// </summary>
public record struct ModRelationshipModel(
    string Id,
    [property: JsonConverter(typeof(SemVersionRangeJsonConverter))]
    SemVersionRange VersionRange,
    ModRelationshipType RelationshipType);

/// <summary>
/// Defines the type of relationship between mods
/// </summary>
public enum ModRelationshipType
{
    /// <summary>
    /// The mod requires this dependency to function
    /// </summary>
    Dependency,
    
    /// <summary>
    /// The mod can use this dependency if available, but it's not required
    /// </summary>
    OptionalDependency,
    
    /// <summary>
    /// The mod is incompatible with this other mod
    /// </summary>
    Incompatible
}