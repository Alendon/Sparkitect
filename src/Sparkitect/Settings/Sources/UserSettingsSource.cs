using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings.Sources;

/// <summary>
/// The writable user override source: a slim in-memory store sitting mid-precedence (below CLI, above
/// engine-config) and acting as the conventional default write target. No persistence today — the future
/// user-settings persistence support (SETG-F01) attaches here.
/// </summary>
[PublicAPI]
public sealed class UserSettingsSource : ISettingSource
{
    private readonly Dictionary<Identification, object?> _values = new();

    /// <summary>
    /// Creates the user source with its mid-precedence ordering against the CLI and engine-config sources.
    /// </summary>
    /// <param name="cliSourceId">The CLI source id this source orders after (lower precedence than CLI).</param>
    /// <param name="engineConfigSourceId">The engine-config source id this source orders before (higher precedence).</param>
    public UserSettingsSource(Identification cliSourceId, Identification engineConfigSourceId)
    {
        // Cross-source references are optional: in a build/runtime where those sources are not registered,
        // the ordering translation drops the edge rather than failing the sort.
        OrderAfter = [new SettingSourceOrder(cliSourceId, Optional: true)];
        OrderBefore = [new SettingSourceOrder(engineConfigSourceId, Optional: true)];
    }

    /// <inheritdoc/>
    public string SourceId => "user";

    /// <inheritdoc/>
    public bool CanWrite => true;

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderBefore { get; }

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderAfter { get; }

    /// <inheritdoc/>
    public bool TryGet(Identification id, out object? value) => _values.TryGetValue(id, out value);

    /// <inheritdoc/>
    public Result<SetError> Write(Identification id, object? value)
    {
        _values[id] = value;
        return new Result<SetError>.Ok();
    }
}
