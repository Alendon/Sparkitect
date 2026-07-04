using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Settings.Sources;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Settings;

public class CliSettingsSourceTests
{
    private static readonly Identification VkValidation = Identification.Create(100, 3, 1);
    private static readonly Identification Unbound = Identification.Create(100, 3, 2);
    private static readonly Identification Levels = Identification.Create(100, 3, 3);
    private static readonly Identification LogLevel = Identification.Create(100, 3, 4);

    private static Func<Identification, ISettingDeclaration?> Declarations(
        params (Identification Id, ISettingDeclaration Declaration)[] entries)
    {
        var map = entries.ToDictionary(entry => entry.Id, entry => entry.Declaration);
        return id => map.GetValueOrDefault(id);
    }

    [Test]
    public async Task ExplicitOption_KeyValue_FeedsParsedValue()
    {
        var source = new CliSettingsSource(
            ["-vkvalidation=false"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: true, CliOption: "vkvalidation"))));

        var supplied = source.TryGet(VkValidation, out var value);

        await Assert.That(supplied).IsTrue();
        await Assert.That((bool)value!).IsFalse();
    }

    [Test]
    public async Task NoDeclaredOption_NeverFed_EvenWhenArgNameMatches()
    {
        // The arg "-unbound=true" is present, but the setting declares no CLI option → not fed (D-11).
        var source = new CliSettingsSource(
            ["-unbound=true"],
            Declarations((Unbound, new SettingDefinition<bool>(Default: false))));

        await Assert.That(source.TryGet(Unbound, out _)).IsFalse();
    }

    [Test]
    public async Task Flag_FeedsTrue_ForBoolSetting()
    {
        var source = new CliSettingsSource(
            ["-vkvalidation"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: false, CliOption: "vkvalidation"))));

        await Assert.That(source.TryGet(VkValidation, out var value)).IsTrue();
        await Assert.That((bool)value!).IsTrue();
    }

    [Test]
    public async Task MultiValue_SemicolonSplit_ParsesFirstScalar()
    {
        var source = new CliSettingsSource(
            ["-levels=3;5;9"],
            Declarations((Levels, new SettingDefinition<int>(Default: 0, CliOption: "levels"))));

        await Assert.That(source.TryGet(Levels, out var value)).IsTrue();
        await Assert.That((int)value!).IsEqualTo(3);
    }

    [Test]
    public async Task DoubleDash_Trimmed_ParityWithRetiredHandler()
    {
        var source = new CliSettingsSource(
            ["--log_level=debug"],
            Declarations((LogLevel, new SettingDefinition<string>(Default: "info", CliOption: "log_level"))));

        await Assert.That(source.TryGet(LogLevel, out var value)).IsTrue();
        await Assert.That((string)value!).IsEqualTo("debug");
    }

    [Test]
    public async Task Write_ReturnsSourceReadonly()
    {
        var source = new CliSettingsSource([], Declarations());

        var result = source.Write(VkValidation, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.SourceReadonly }).IsTrue();
    }
}
