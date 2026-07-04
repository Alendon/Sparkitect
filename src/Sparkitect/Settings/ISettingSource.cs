using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings;

/// <summary>
/// A single ordering constraint between setting sources: the annotated source is placed relative to
/// <see cref="Target"/>. A missing required target fails loud; an optional one is dropped.
/// </summary>
/// <param name="Target">The source this constraint orders against.</param>
/// <param name="Optional">When true, the constraint is dropped if <paramref name="Target"/> is not registered.</param>
[PublicAPI]
public readonly record struct SettingSourceOrder(Identification Target, bool Optional = false);

/// <summary>
/// A setting source owns its own value acquisition. Sources are registry-declared and Identification-keyed;
/// resolution walks the ordered source list and the first source that supplies an explicit value wins.
/// </summary>
[PublicAPI]
public interface ISettingSource
{
    /// <summary>Stable string id ranking this source deterministically in the ordering tiebreak.</summary>
    string SourceId { get; }

    /// <summary>Whether this source accepts writes. Readonly sources (CLI, engine-config) return false.</summary>
    bool CanWrite { get; }

    /// <summary>
    /// Sources this source is ordered before (higher precedence than the targets). Read when the
    /// registration pass is processed, not at registration: implementations whose targets are generated
    /// ids assigned during the same pass must evaluate lazily rather than capture at construction.
    /// </summary>
    IReadOnlyList<SettingSourceOrder> OrderBefore { get; }

    /// <summary>Sources this source is ordered after (lower precedence than the targets). Same deferred read as <see cref="OrderBefore"/>.</summary>
    IReadOnlyList<SettingSourceOrder> OrderAfter { get; }

    /// <summary>Acquires this source's explicit value for <paramref name="id"/>, if it supplies one.</summary>
    /// <param name="id">The setting to acquire.</param>
    /// <param name="value">The acquired value when this source supplies one; otherwise null.</param>
    /// <returns>True when this source supplies an explicit value.</returns>
    bool TryGet(Identification id, out object? value);

    /// <summary>Writes <paramref name="value"/> for <paramref name="id"/>. Readonly sources return <see cref="SetError.SourceReadonly"/>.</summary>
    /// <param name="id">The setting to write.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>Ok on success, or a <see cref="SetError"/> when the source refuses the write.</returns>
    Result<SetError> Write(Identification id, object? value);
}
