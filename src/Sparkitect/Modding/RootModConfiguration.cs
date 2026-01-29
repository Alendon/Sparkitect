using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;
using Sparkitect.Utils;

namespace Sparkitect.Modding;

/// <summary>
/// Configuration specifying which root mods to load at engine startup.
/// </summary>
/// <param name="RootMods">List of root mods to load.</param>
public record RootModConfig(IReadOnlyList<RootModEntry> RootMods);

/// <summary>
/// Entry specifying a root mod to load.
/// </summary>
/// <param name="Id">Mod identifier.</param>
/// <param name="Version">Optional specific version. If null, load newest available version.</param>
public record RootModEntry(
    string Id,
    [property: JsonConverter(typeof(SemVersionJsonConverter))]
    SemVersion? Version = null);

/// <summary>
/// Provides loading functionality for root mod configuration files.
/// </summary>
/// <remarks>
/// <para>
/// Important distinction between Discovery and Loading:
/// </para>
/// <list type="bullet">
///   <item><description>Discovery always finds ALL mods regardless of configuration</description></item>
///   <item><description>This config file controls which discovered mods to LOAD as roots at startup</description></item>
/// </list>
/// <para>
/// When config file doesn't exist, bootstrapper falls back to loading all discovered mods
/// that have IsRootMod = true in their manifest.
/// </para>
/// </remarks>
public static class RootModConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new SemVersionJsonConverter() }
    };

    /// <summary>
    /// Loads root mod configuration from a JSON file.
    /// </summary>
    /// <param name="path">Path to the configuration file.</param>
    /// <returns>
    /// Parsed configuration if file exists and is valid, null if file doesn't exist.
    /// </returns>
    /// <exception cref="JsonException">Thrown when JSON is malformed or cannot be deserialized.</exception>
    /// <remarks>
    /// Returns null when file doesn't exist - this is not an error.
    /// The bootstrapper interprets null as "load all discovered mods with IsRootMod = true".
    /// </remarks>
    public static RootModConfig? LoadConfig(string path)
    {
        if (!File.Exists(path))
            return null; // No config = fallback to discovering all root mods

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RootModConfig>(json, JsonOptions);
    }
}
