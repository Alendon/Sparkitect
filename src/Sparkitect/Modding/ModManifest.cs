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
/// <param name="IsRootMod">Whether this mod can be loaded as a root mod at engine startup. Default is false.</param>
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
    IReadOnlyList<string> RequiredAssemblies,
    bool IsRootMod = false);

/// <summary>
/// Defines a relationship between mods.
/// </summary>
/// <remarks>
/// Relationship semantics based on boolean properties:
/// <list type="bullet">
///   <item><description>Default (IsOptional=false, IsIncompatible=false) = Required dependency - mod cannot load without it.</description></item>
///   <item><description>IsOptional=true = Optional dependency - mod can load without it but will use it if present.</description></item>
///   <item><description>IsIncompatible=true = Incompatibility marker - mod cannot load if this mod is present.</description></item>
/// </list>
/// </remarks>
/// <param name="Id">Target mod identifier.</param>
/// <param name="VersionRange">Acceptable version range using semantic versioning.</param>
/// <param name="IsOptional">Whether this dependency is optional. Default is false (required).</param>
/// <param name="IsIncompatible">Whether this marks an incompatibility. Default is false.</param>
public record struct ModRelationship(
    string Id,
    [property: JsonConverter(typeof(SemVersionRangeJsonConverter))]
    SemVersionRange VersionRange,
    bool IsOptional = false,
    bool IsIncompatible = false);
