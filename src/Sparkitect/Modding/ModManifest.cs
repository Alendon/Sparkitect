using System.Text.Json.Serialization;
using Semver;
using Sparkitect.Utils;

namespace Sparkitect.Modding;

/// <summary>
/// Mod metadata loaded from mod manifest file.
/// </summary>
/// <param name="Id">Unique mod identifier.</param>
/// <param name="Name">Human-readable mod name.</param>
/// <param name="Description">Mod description.</param>
/// <param name="Version">Semantic version.</param>
/// <param name="Authors">List of mod authors.</param>
/// <param name="ModPath">File system path to mod archive (set at runtime, not in manifest).</param>
/// <param name="Relationships">Mod dependencies and incompatibilities.</param>
/// <param name="ModAssembly">Primary mod assembly file name.</param>
/// <param name="RequiredAssemblies">Additional required assembly file names.</param>
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

/// <summary>
/// Defines a relationship between mods (dependency, optional dependency, or incompatibility).
/// </summary>
/// <param name="Id">Target mod identifier.</param>
/// <param name="VersionRange">Acceptable version range using semantic versioning.</param>
/// <param name="RelationshipType">Type of relationship.</param>
public record struct ModRelationship(
    string Id,
    [property: JsonConverter(typeof(SemVersionRangeJsonConverter))]
    SemVersionRange VersionRange,
    ModRelationshipType RelationshipType);

/// <summary>
/// Type of relationship between mods.
/// </summary>
public enum ModRelationshipType
{
    /// <summary>
    /// Required dependency - mod cannot load without it.
    /// </summary>
    Dependency,

    /// <summary>
    /// Optional dependency - mod can load without it but will use it if present.
    /// </summary>
    OptionalDependency,

    /// <summary>
    /// Incompatibility - mod cannot load if this mod is present.
    /// </summary>
    Incompatible
}