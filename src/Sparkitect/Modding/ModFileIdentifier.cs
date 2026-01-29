using Semver;

namespace Sparkitect.Modding;

/// <summary>
/// Uniquely identifies a mod by its ID and version.
/// </summary>
/// <remarks>
/// <para>
/// Used for explicit mod selection when loading mods. This ensures unambiguous
/// selection when multiple versions of the same mod may exist.
/// </para>
/// <para>
/// Designed for future extensibility (e.g., debug/release tags) by using
/// a struct with named properties rather than a tuple.
/// </para>
/// </remarks>
public readonly struct ModFileIdentifier : IEquatable<ModFileIdentifier>
{
    /// <summary>
    /// Mod identifier (snake_case convention).
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Semantic version of the mod.
    /// </summary>
    public SemVersion Version { get; }

    /// <summary>
    /// Creates a new ModFileIdentifier.
    /// </summary>
    /// <param name="id">Mod identifier.</param>
    /// <param name="version">Semantic version.</param>
    /// <exception cref="ArgumentNullException">Thrown when id or version is null.</exception>
    public ModFileIdentifier(string id, SemVersion version)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(version);

        Id = id;
        Version = version;
    }

    /// <summary>
    /// Parses a ModFileIdentifier from string format "mod_id@version".
    /// </summary>
    /// <param name="value">String in format "mod_id@version" (e.g., "my_mod@1.0.0").</param>
    /// <returns>Parsed ModFileIdentifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when value is not in valid format.</exception>
    public static ModFileIdentifier Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var atIndex = value.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == value.Length - 1)
            throw new FormatException($"Invalid ModFileIdentifier format: '{value}'. Expected format: 'mod_id@version'");

        var id = value[..atIndex];
        var versionString = value[(atIndex + 1)..];

        if (!SemVersion.TryParse(versionString, SemVersionStyles.Any, out var version))
            throw new FormatException($"Invalid version in ModFileIdentifier: '{versionString}'");

        return new ModFileIdentifier(id, version);
    }

    /// <summary>
    /// Tries to parse a ModFileIdentifier from string format "mod_id@version".
    /// </summary>
    /// <param name="value">String in format "mod_id@version" (e.g., "my_mod@1.0.0").</param>
    /// <param name="result">Parsed ModFileIdentifier if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? value, out ModFileIdentifier result)
    {
        result = default;

        if (string.IsNullOrEmpty(value))
            return false;

        var atIndex = value.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == value.Length - 1)
            return false;

        var id = value[..atIndex];
        var versionString = value[(atIndex + 1)..];

        if (!SemVersion.TryParse(versionString, SemVersionStyles.Any, out var version))
            return false;

        result = new ModFileIdentifier(id, version);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(ModFileIdentifier other) =>
        string.Equals(Id, other.Id, StringComparison.Ordinal) &&
        Equals(Version, other.Version);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ModFileIdentifier other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Version);

    /// <summary>
    /// Returns string representation in format "mod_id@version".
    /// </summary>
    public override string ToString() => $"{Id}@{Version}";

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(ModFileIdentifier left, ModFileIdentifier right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(ModFileIdentifier left, ModFileIdentifier right) => !left.Equals(right);
}
