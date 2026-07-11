using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Settings.Sources;

namespace Sparkitect.Tests.Settings;

public class ModdingSettingsTests
{
    private static readonly Identification<AlcGranularity> AlcGranularityId =
        new(Identification.Create(102, 1, 1));
    private static readonly Identification<bool> UnloadWaitId = new(Identification.Create(102, 1, 2));
    private static readonly Identification UserSourceId = Identification.Create(102, 2, 1);

    [Test]
    public async Task AlcGranularity_NoSource_ResolvesToPerGroupDefault()
    {
        var manager = new SettingsManager();
        manager.Declare(AlcGranularityId, new SettingDefinition<AlcGranularity>(Default: AlcGranularity.PerGroup));

        await Assert.That(manager.GetSetting(AlcGranularityId).Value).IsEqualTo(AlcGranularity.PerGroup);
    }

    [Test]
    public async Task AlcGranularity_CliSource_ParsesCaseInsensitive()
    {
        Identification bareId = AlcGranularityId;
        var declaration = new SettingDefinition<AlcGranularity>(
            Default: AlcGranularity.PerGroup, CliOption: "mod-alc-granularity");
        Func<Identification, ISettingDeclaration?> declarations = id => id == bareId ? declaration : null;

        var upperSource = new CliSettingsSource(["--mod-alc-granularity=PerMod"], declarations);
        var lowerSource = new CliSettingsSource(["--mod-alc-granularity=permod"], declarations);

        await Assert.That(upperSource.TryGet(bareId, out var upperValue)).IsTrue();
        await Assert.That((AlcGranularity)upperValue!).IsEqualTo(AlcGranularity.PerMod);

        await Assert.That(lowerSource.TryGet(bareId, out var lowerValue)).IsTrue();
        await Assert.That((AlcGranularity)lowerValue!).IsEqualTo(AlcGranularity.PerMod);
    }

    [Test]
    public async Task UnloadWaitOnShutdown_NoSource_ResolvesTrueDefault()
    {
        var manager = new SettingsManager();
        manager.Declare(UnloadWaitId, new SettingDefinition<bool>(Default: true));

        await Assert.That(manager.GetSetting(UnloadWaitId).Value).IsTrue();
    }

    [Test]
    public async Task UnloadWaitOnShutdown_StubSource_OverridesToFalse()
    {
        var manager = new SettingsManager();
        manager.Declare(UnloadWaitId, new SettingDefinition<bool>(Default: true));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true,
            values: new Dictionary<Identification, object> { [UnloadWaitId] = false }));

        await Assert.That(manager.GetSetting(UnloadWaitId).Value).IsFalse();
    }
}
