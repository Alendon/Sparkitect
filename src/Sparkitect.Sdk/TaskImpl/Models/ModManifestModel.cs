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
/// Represents a relationship between mods.
/// </summary>
/// <remarks>
/// Relationship semantics based on boolean properties:
/// <list type="bullet">
///   <item><description>Default (IsOptional=false, IsIncompatible=false) = Required dependency.</description></item>
///   <item><description>IsOptional=true = Optional dependency.</description></item>
///   <item><description>IsIncompatible=true = Incompatibility marker.</description></item>
/// </list>
/// </remarks>
/// <param name="Id">Target mod identifier.</param>
/// <param name="VersionRange">Acceptable version range using semantic versioning.</param>
/// <param name="IsOptional">Whether this dependency is optional. Default is false (required).</param>
/// <param name="IsIncompatible">Whether this marks an incompatibility. Default is false.</param>
public record struct ModRelationshipModel(
    string Id,
    [property: JsonConverter(typeof(SemVersionRangeJsonConverter))]
    SemVersionRange VersionRange,
    bool IsOptional = false,
    bool IsIncompatible = false);
