using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Settings.Sources;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Settings;

public class CliSettingsSourceTests
{
    private static readonly Identification VkValidation = Identification.Create(100, 3, 1);
    private static readonly Identification Unbound = Identification.Create(100, 3, 2);
    private static readonly Identification Count = Identification.Create(100, 3, 3);

    private static Func<Identification, ISettingDeclaration?> Declarations(
        params (Identification Id, ISettingDeclaration Declaration)[] entries)
    {
        var map = entries.ToDictionary(entry => entry.Id, entry => entry.Declaration);
        return id => map.GetValueOrDefault(id);
    }

    [Test]
    public async Task KeyValue_FeedsParsedValue()
    {
        var source = new CliSettingsSource(
            ["--vk-validation=false"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: true, CliOption: "vk-validation"))));

        var supplied = source.TryGet(VkValidation, out var value);

        await Assert.That(supplied).IsTrue();
        await Assert.That((bool)value!).IsFalse();
    }

    [Test]
    public async Task BareFlag_FeedsTrue_ForBoolSetting()
    {
        var source = new CliSettingsSource(
            ["--vk-validation"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: false, CliOption: "vk-validation"))));

        await Assert.That(source.TryGet(VkValidation, out var value)).IsTrue();
        await Assert.That((bool)value!).IsTrue();
    }

    [Test]
    public async Task NegatedFlag_FeedsFalse_ForBoolSetting()
    {
        var source = new CliSettingsSource(
            ["--no-vk-validation"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: true, CliOption: "vk-validation"))));

        await Assert.That(source.TryGet(VkValidation, out var value)).IsTrue();
        await Assert.That((bool)value!).IsFalse();
    }

    [Test]
    public async Task NoDeclaredOption_NeverFed_EvenWhenArgNameMatches()
    {
        var source = new CliSettingsSource(
            ["--unbound=true"],
            Declarations((Unbound, new SettingDefinition<bool>(Default: false))));

        await Assert.That(source.TryGet(Unbound, out _)).IsFalse();
    }

    [Test]
    public async Task RepeatedValue_AccumulatesIntoMulti()
    {
        var parsed = CliSettingsSource.ParseArguments(["--x=a", "--x=b"]);

        await Assert.That(parsed["x"] is CliArgValue.Multi { Values: ["a", "b"] }).IsTrue();
    }

    [Test]
    public async Task SemicolonInValue_IsLiteral_NotSplit()
    {
        var parsed = CliSettingsSource.ParseArguments(["--x=a;b"]);

        await Assert.That(parsed["x"] is CliArgValue.Single { Value: "a;b" }).IsTrue();
    }

    [Test]
    public async Task KeyMatching_IsCaseSensitive()
    {
        var source = new CliSettingsSource(
            ["--Vk-Validation"],
            Declarations((VkValidation, new SettingDefinition<bool>(Default: false, CliOption: "vk-validation"))));

        await Assert.That(source.TryGet(VkValidation, out _)).IsFalse();
    }

    [Test]
    public async Task RepeatedValue_PulledByScalarSetting_FailsLoud()
    {
        var source = new CliSettingsSource(
            ["--count=1", "--count=2"],
            Declarations((Count, new SettingDefinition<int>(Default: 0, CliOption: "count"))));

        await Assert.That(() => source.TryGet(Count, out _)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task BareFlag_PulledByNonBoolSetting_FailsLoud()
    {
        var source = new CliSettingsSource(
            ["--count"],
            Declarations((Count, new SettingDefinition<int>(Default: 0, CliOption: "count"))));

        await Assert.That(() => source.TryGet(Count, out _)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task UnparseableValue_FailsLoud()
    {
        var source = new CliSettingsSource(
            ["--count=notanumber"],
            Declarations((Count, new SettingDefinition<int>(Default: 0, CliOption: "count"))));

        await Assert.That(() => source.TryGet(Count, out _)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SingleDashToken_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["-count=1"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BarePositionalToken_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["count"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EmptyKeyToken_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["--=value"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BareDoubleDash_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["--"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NegatedFormWithValue_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["--no-vk-validation=false"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task FlagAndNegatedFlagConflict_FailsLoud()
    {
        await Assert.That(() => new CliSettingsSource(["--vk-validation", "--no-vk-validation"], Declarations()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UnknownOptionName_RetainedSilently()
    {
        var parsed = CliSettingsSource.ParseArguments(["--totally-unknown=1"]);

        await Assert.That(parsed.ContainsKey("totally-unknown")).IsTrue();
    }

    [Test]
    public async Task DeclaredOptionWithReservedNegationPrefix_FailsLoud()
    {
        await Assert.That(() => new SettingDefinition<bool>(Default: false, CliOption: "no-thing"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_ReturnsSourceReadonly()
    {
        var source = new CliSettingsSource([], Declarations());

        var result = source.Write(VkValidation, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.SourceReadonly }).IsTrue();
    }
}
