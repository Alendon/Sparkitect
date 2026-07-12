using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Tests.Modding;

// Guards the group single-ownership invariant: a settings group id is owned by
// exactly one container. A second ownership declaration of an already-owned group id fails loud
// (SPARK0270) — ownership is a generation-time concept (groups have no runtime registry), so the fail
// is a generator diagnostic. Adding accessor members onto an existing group is fine; re-declaring
// ownership is the clash.
public class SettingsGroupOwnershipTests : SourceGeneratorTestBase<SettingsAccessorGenerator>
{
    private const string GroupAttributeSource = """
        namespace Sparkitect.Settings
        {
            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public sealed class SettingGroupAttribute : System.Attribute
            {
                public SettingGroupAttribute(string group) { Group = group; }
                public string Group { get; }
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
    public async Task SecondOwnershipDeclaration_OfOwnedGroup_FailsLoud(CancellationToken token)
    {
        TestSources.Add(("GroupAttribute.cs", GroupAttributeSource));
        TestSources.Add(("DuplicateGroups.cs", """
            namespace Sparkitect.Settings.Groups
            {
                [Sparkitect.Settings.SettingGroup("graphics")]
                public readonly partial struct GraphicsSettings { }

                [Sparkitect.Settings.SettingGroup("graphics")]
                public readonly partial struct RogueGraphicsSettings { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        // The duplicate ownership declaration fails loud, and the message identifies the clashing group.
        var clash = driverRunResult.Diagnostics.FirstOrDefault(d => d.Id == "SPARK0270");
        await Assert.That(clash).IsNotNull()
            .Because("A second ownership declaration of the 'graphics' group must report SPARK0270.");
        await Assert.That(clash!.GetMessage()).Contains("graphics");
    }

    [Test]
    public async Task SingleOwnershipDeclaration_DoesNotFail(CancellationToken token)
    {
        // A single declaration is not a clash — the guard fires only on a SECOND ownership declaration,
        // never on a lone valid one (this pins the clash behavior, not registry-internal storage).
        TestSources.Add(("GroupAttribute.cs", GroupAttributeSource));
        TestSources.Add(("SingleGroup.cs", """
            namespace Sparkitect.Settings.Groups
            {
                [Sparkitect.Settings.SettingGroup("graphics")]
                public readonly partial struct GraphicsSettings { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        await Assert.That(driverRunResult.Diagnostics.Any(d => d.Id == "SPARK0270"))
            .IsFalse().Because("A single group-ownership declaration must not fail loud.");
    }
}
