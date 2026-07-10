using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings;

/// <summary>
/// The settings engine: declares settings, registers ordered sources, resolves effective values, and
/// dispatches effective-change callbacks. This hand-written typed surface works standalone; the
/// source-generated typed accessor hierarchy is deferred sugar layered on top of it.
/// </summary>
[PublicAPI]
public interface ISettingsManager
{
    /// <summary>Declares a setting with its default and CLI binding. Called by the settings registry.</summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    /// <param name="definition">The declaration payload carrying the default and optional CLI option.</param>
    void Declare<T>(Identification<T> id, SettingDefinition<T> definition);

    /// <summary>Removes a previously declared setting. Called by the settings registry on teardown.</summary>
    /// <param name="id">The setting id to undeclare.</param>
    void Undeclare(Identification id);

    /// <summary>
    /// Returns the non-generic declaration view for <paramref name="id"/>, or null when undeclared.
    /// Readonly sources use it to read a setting's CLI option and parse a raw scalar against its type.
    /// </summary>
    /// <param name="id">The setting id.</param>
    ISettingDeclaration? GetDeclaration(Identification id);

    /// <summary>Records a value source; the resolution order is recomputed after the registration pass.</summary>
    /// <param name="id">The source id.</param>
    /// <param name="source">The source instance.</param>
    void RegisterSource(Identification id, ISettingSource source);

    /// <summary>Returns a lightweight typed handle over the manager for <paramref name="id"/>.</summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    Setting<T> GetSetting<T>(Identification<T> id);

    /// <summary>Resolves the effective value of <paramref name="id"/> over the ordered source list.</summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    T GetValue<T>(Identification<T> id);

    /// <summary>
    /// Writes <paramref name="value"/> to the source <paramref name="sourceId"/>, then dispatches
    /// effective-change callbacks only when the resolved value actually changed.
    /// </summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    /// <param name="sourceId">The target source id.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Ok on success, or a <see cref="SetError"/> arm when the write is refused.</returns>
    Result<SetError> Set<T>(Identification<T> id, Identification sourceId, T value);

    /// <summary>Writes <paramref name="value"/> to the conventional writable (user) source.</summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Ok on success, or a <see cref="SetError"/> arm when no writable source is available.</returns>
    Result<SetError> SetUserValue<T>(Identification<T> id, T value);

    /// <summary>
    /// Subscribes to effective-value changes of <paramref name="id"/>. The subscription binds to the
    /// current state frame at subscribe time and is cleared wholesale on that frame's teardown.
    /// </summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    /// <param name="onEffectiveChange">Invoked synchronously with the new resolved value on effective change.</param>
    /// <returns>A handle that removes the subscription when disposed.</returns>
    IDisposable Subscribe<T>(Identification<T> id, Action<T> onEffectiveChange);
}
