using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings;

/// <summary>
/// A lightweight, freely-constructible typed handle over an <see cref="ISettingsManager"/> and a setting
/// id. It stores nothing but the manager reference and the id — reads resolve through the manager on
/// every access, so a handle is not an identity-managed object and holds no cached value.
/// </summary>
/// <typeparam name="T">The setting's primitive value type.</typeparam>
[PublicAPI]
public readonly struct Setting<T>
{
    private readonly ISettingsManager _manager;
    private readonly Identification<T> _id;

    /// <summary>Creates a handle over <paramref name="manager"/> for setting <paramref name="id"/>.</summary>
    /// <param name="manager">The owning settings manager.</param>
    /// <param name="id">The setting id.</param>
    public Setting(ISettingsManager manager, Identification<T> id)
    {
        _manager = manager;
        _id = id;
    }

    /// <summary>The current effective value, resolved through the manager on each access.</summary>
    public T Value => _manager.GetValue<T>(_id);

    /// <summary>Writes a value to the conventional writable (user) source through the manager.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Ok on success, or a <see cref="SetError"/> arm when the write is refused.</returns>
    public Result<SetError> Set(T value) => _manager.SetUserValue(_id, value);

    /// <summary>Registers an effective-change callback bound to the current state frame.</summary>
    /// <param name="callback">Invoked with the new resolved value when the effective value changes.</param>
    /// <returns>A handle that removes the subscription when disposed.</returns>
    public IDisposable OnChanged(Action<T> callback) => _manager.Subscribe(_id, callback);
}
