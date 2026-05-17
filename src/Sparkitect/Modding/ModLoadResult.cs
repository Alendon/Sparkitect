using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Modding;

/// <summary>
/// Validation error types using Sundew.DiscriminatedUnions.
/// </summary>
/// <remarks>
/// Uses Sundew.DiscriminatedUnions for future compatibility with native .NET discriminated unions.
/// </remarks>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ValidationError
{
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public abstract string Message { get; }

    /// <summary>
    /// A required dependency was not found among available mods.
    /// </summary>
    public sealed partial record MissingDependency(string ModId, string DependencyId) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' requires dependency '{DependencyId}' which is not available.";
    }

    /// <summary>
    /// A dependency exists but its version doesn't match the required range.
    /// </summary>
    public sealed partial record VersionMismatch(string ModId, string DependencyId, string Expected, string Found) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' requires '{DependencyId}' version {Expected}, but found version {Found}.";
    }

    /// <summary>
    /// A mod specified in root config is not marked as a root mod in its manifest.
    /// </summary>
    public sealed partial record NotRootMod(string ModId) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' is specified in root config but is not marked as a root mod in its manifest.";
    }

    /// <summary>
    /// A specified mod was not found in discovered mods.
    /// </summary>
    public sealed partial record NotFound(string ModId) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' was not found in discovered mods.";
    }

    /// <summary>
    /// A mod declares a dependency on itself.
    /// </summary>
    public sealed partial record SelfReference(string ModId) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' declares a dependency on itself.";
    }

    /// <summary>
    /// A mod is incompatible with another loaded mod.
    /// </summary>
    public sealed partial record IncompatibleMod(string ModId, string IncompatibleId, string Version) : ValidationError
    {
        /// <inheritdoc />
        public override string Message => $"Mod '{ModId}' is incompatible with '{IncompatibleId}' version {Version}.";
    }
}
