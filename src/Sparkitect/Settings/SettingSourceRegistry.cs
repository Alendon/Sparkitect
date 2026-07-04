using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Settings;

/// <summary>
/// Registry for setting value sources. Each registration records a source instance (carrying its own
/// acquisition and OrderBefore/OrderAfter metadata) into the manager; the resolution order is
/// recomputed once after the registration pass completes.
/// </summary>
[Registry(Identifier = "setting_source")]
[PublicAPI]
public partial class SettingSourceRegistry(ISettingsManager manager) : IRegistry<CoreModule>
{
    /// <inheritdoc/>
    public static string Identifier => "setting_source";

    /// <summary>Registers a value source under <paramref name="id"/>.</summary>
    /// <param name="id">The source id.</param>
    /// <param name="source">The source instance.</param>
    [RegistryMethod]
    public void RegisterSource(Identification id, ISettingSource source) => manager.RegisterSource(id, source);

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
    }
}
