using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Settings;

/// <summary>
/// Generic value registry for setting declarations. A mod's provider returns a
/// <see cref="SettingDefinition{T}"/> carrying the default; the closed generic value type survives
/// registration and reaches <see cref="ISettingsManager.Declare{T}"/> with its type intact.
/// </summary>
[Registry(Identifier = "setting")]
[PublicAPI]
public partial class SettingRegistry(ISettingsManager manager) : IRegistry<CoreModule>
{
    /// <inheritdoc/>
    public static string Identifier => "setting";

    /// <summary>Declares a setting: binds <paramref name="id"/> to <paramref name="definition"/>.</summary>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    /// <param name="id">The setting id.</param>
    /// <param name="definition">The declaration payload carrying the default and optional CLI option.</param>
    [RegistryMethod]
    public void RegisterSetting<[TypedIdentification] T>(Identification id, SettingDefinition<T> definition) =>
        manager.Declare(new Identification<T>(id), definition);

    /// <inheritdoc/>
    public void Unregister(Identification id) => manager.Undeclare(id);
}
