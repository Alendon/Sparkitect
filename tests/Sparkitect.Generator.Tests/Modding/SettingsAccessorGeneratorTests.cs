using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Tests.Modding;

// Guards the 57-05 SG accessor hierarchy: the generator emits settingsManager.<Group>.<Setting>
// members returning Setting<T> handles that delegate to the hand-written GetSetting<T> path — pure
// sugar over the standalone typed API (D-02).
public class SettingsAccessorGeneratorTests : SourceGeneratorTestBase<SettingsAccessorGenerator>
{
    // Minimal declaration surface: the settings-accessor attributes, a SettingDefinition<T> stub, a
    // single-owner group container, and a provider bound to that group.
    private const string DeclarationSurface = """
        namespace Sparkitect.Settings
        {
            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public sealed class SettingGroupAttribute : System.Attribute
            {
                public SettingGroupAttribute(string group) { Group = group; }
                public string Group { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Method)]
            public sealed class SettingAccessorAttribute : System.Attribute
            {
                public SettingAccessorAttribute(string group, string name, string settingId)
                {
                    Group = group; Name = name; SettingId = settingId;
                }
                public string Group { get; }
                public string Name { get; }
                public string SettingId { get; }
            }

            public sealed class SettingDefinition<T>
            {
                public SettingDefinition(T @default) { Default = @default; }
                public T Default { get; }
            }
        }

        namespace Sparkitect.Settings.Groups
        {
            [Sparkitect.Settings.SettingGroup("graphics")]
            public readonly partial struct GraphicsSettings { }
        }

        namespace EngineTest
        {
            public static class Providers
            {
                [Sparkitect.Settings.SettingAccessor("graphics", "VulkanValidation", "vulkan_validation")]
                public static Sparkitect.Settings.SettingDefinition<bool> VulkanValidation
                    => new Sparkitect.Settings.SettingDefinition<bool>(true);
            }
        }
        """;

    [Before(Test)]
    public void Setup()
    {
        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = Sample Test Mod
            build_property.ModId = sample_test
            build_property.RootNamespace = SampleTest
            build_property.SgOutputNamespace = SampleTest.Generated
            """));
    }

    [Test]
    public async Task Generator_EmitsGroupFirstAccessor_DelegatingToGetSetting(CancellationToken token)
    {
        TestSources.Add(("DeclarationSurface.cs", DeclarationSurface));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var trees = driverRunResult.GeneratedTrees
            .ToDictionary(t => Path.GetFileName(t.FilePath), t => t.GetText().ToString());
        var allFiles = string.Join(", ", trees.Keys.OrderBy(f => f));

        await Assert.That(trees.ContainsKey("SettingsAccessors.g.cs"))
            .IsTrue().Because($"Generated files: {allFiles}");

        var code = trees["SettingsAccessors.g.cs"];

        // (1) The middle-level group accessor is emitted over an INSTANCE ISettingsManager receiver.
        await Assert.That(code).Contains("extension(global::Sparkitect.Settings.ISettingsManager manager)");
        await Assert.That(code).Contains("global::Sparkitect.Settings.Groups.GraphicsSettings Graphics");

        // (2) The setting-level member returns a Setting<T> handle (T recovered from SettingDefinition<T>).
        await Assert.That(code)
            .Contains("public global::Sparkitect.Settings.Setting<bool> VulkanValidation");

        // (3) It delegates to the hand-written GetSetting<T> path against the generated setting id —
        // the emitted accessor is sugar, not a new resolution path.
        await Assert.That(code).Contains(".GetSetting<bool>(");
        await Assert.That(code)
            .Contains("global::Sparkitect.Modding.IDs.SettingID.SampleTest.VulkanValidation");
    }
}
