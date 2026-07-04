using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Settings.Sources;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Settings;

public class EngineSettingsSourceTests
{
    private static readonly Identification VulkanValidation = Identification.Create(101, 1, 1);
    private static readonly Identification FpsCap = Identification.Create(101, 1, 2);
    private static readonly Identification Absent = Identification.Create(101, 1, 3);

    private const string Yaml = """
        vulkan_validation: false
        fps_cap: 144
        """;

    private static string? KeyOf(Identification id) =>
        id == VulkanValidation ? "vulkan_validation"
        : id == FpsCap ? "fps_cap"
        : id == Absent ? "missing_key"
        : null;

    private static ISettingDeclaration? DeclarationOf(Identification id) =>
        id == VulkanValidation ? new SettingDefinition<bool>(Default: true)
        : id == FpsCap ? new SettingDefinition<int>(Default: 0)
        : id == Absent ? new SettingDefinition<bool>(Default: true)
        : null;

    private static EngineSettingsSource NewSource(string? yaml = Yaml) =>
        new(yaml, KeyOf, DeclarationOf);

    [Test]
    public async Task Scalar_ResolvesToDeclaredBool()
    {
        var source = NewSource();

        await Assert.That(source.TryGet(VulkanValidation, out var value)).IsTrue();
        await Assert.That((bool)value!).IsFalse();
    }

    [Test]
    public async Task Scalar_ResolvesToDeclaredInt()
    {
        var source = NewSource();

        await Assert.That(source.TryGet(FpsCap, out var value)).IsTrue();
        await Assert.That((int)value!).IsEqualTo(144);
    }

    [Test]
    public async Task AbsentKey_SuppliesNothing()
    {
        var source = NewSource();

        await Assert.That(source.TryGet(Absent, out _)).IsFalse();
    }

    [Test]
    public async Task NoYaml_SuppliesNothing()
    {
        var source = NewSource(yaml: null);

        await Assert.That(source.TryGet(VulkanValidation, out _)).IsFalse();
    }

    [Test]
    public async Task Write_ReturnsSourceReadonly()
    {
        var source = NewSource();

        var result = source.Write(VulkanValidation, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.SourceReadonly }).IsTrue();
    }
}
