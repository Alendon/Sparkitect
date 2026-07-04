using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding;
using Sparkitect.Generator.Modding.Analyzers;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

// Guards the D-18 generator relaxation (57-01): a register method whose value parameter is a
// constructed generic over the method type parameter — RegisterSetting<T>(Identification,
// SettingDefinition<T>) — generates its attribute and preserves the closed generic value type.
// These would fail against the pre-relaxation generator, where the wrapper method was silently
// skipped (no attribute) and the shape analyzer reported SPARK0213 GenericValueMismatch.
public class RegistryGeneratorGenericWrapperTests : SourceGeneratorTestBase<RegistryGenerator>
{
    // Registry declaring the wrapper-over-T register method + a SettingDefinition<T> stub + a
    // static property provider returning the closed SettingDefinition<bool>.
    private const string WrapperRegistrySource = """
        using Sparkitect.DI.GeneratorAttributes;
        using Sparkitect.Modding;

        namespace DiTest
        {
            public sealed class SettingDefinition<T>
            {
                public SettingDefinition(T defaultValue) => DefaultValue = defaultValue;
                public T DefaultValue { get; }
            }

            [Registry(Identifier = "setting")]
            public class SettingRegistry : IRegistry<TestModule>
            {
                [RegistryMethod]
                public void RegisterSetting<T>(Identification id, SettingDefinition<T> definition) { }
            }

            public static class Providers
            {
                [SettingRegistry.RegisterSetting("vulkan_validation")]
                public static SettingDefinition<bool> VulkanValidation => new SettingDefinition<bool>(true);
            }
        }
        """;

    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);

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
    public async Task WrapperRegisterMethod_GeneratesAttribute_AndPreservesClosedGeneric(CancellationToken token)
    {
        TestSources.Add(("WrapperRegistry.cs", WrapperRegistrySource));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var trees = driverRunResult.GeneratedTrees
            .ToDictionary(t => Path.GetFileName(t.FilePath), t => t.GetText().ToString());
        var allFiles = string.Join(", ", trees.Keys.OrderBy(f => f));

        // (1) The wrapper register method is no longer silently skipped: its attribute is generated.
        await Assert.That(trees.ContainsKey("SettingRegistry_Attributes.g.cs"))
            .IsTrue().Because($"Generated files: {allFiles}");
        await Assert.That(trees["SettingRegistry_Attributes.g.cs"])
            .Contains("class RegisterSettingAttribute");

        // (2) The provider registration entry carries the closed generic SettingDefinition<bool>:
        // the emitted call is RegisterSetting(id, value) with NO explicit type argument, so C# infers
        // T = bool from the provider's return type (never erased to an explicit <object>/<T>).
        var registrationEntry = trees
            .Where(kv => kv.Value.Contains(".RegisterSetting(") && kv.Value.Contains("VulkanValidation"))
            .Select(kv => kv.Value)
            .FirstOrDefault();
        await Assert.That(registrationEntry)
            .IsNotNull().Because($"No generated registration call found. Generated files: {allFiles}");
        await Assert.That(registrationEntry!).Contains(".RegisterSetting(");
        // No erased/explicit type argument on the emitted registration call.
        await Assert.That(registrationEntry!).DoesNotContain(".RegisterSetting<");
    }
}

// The shape analyzer must NOT report SPARK0213 (GenericValueMismatch) for the wrapper-over-T
// register method — it is a valid shape after the D-18 relaxation.
public class RegistryShapeAnalyzerGenericWrapperTests : AnalyzerTestBase<RegistryShapeAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);
    }

    [Test]
    public async Task WrapperRegisterMethod_DoesNotReportGenericValueMismatch(CancellationToken token)
    {
        TestSources.Add(("WrapperRegistry.cs", """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest
            {
                public sealed class SettingDefinition<T>
                {
                    public SettingDefinition(T defaultValue) => DefaultValue = defaultValue;
                    public T DefaultValue { get; }
                }

                [Registry(Identifier = "setting")]
                public class SettingRegistry : IRegistry<TestModule>
                {
                    [RegistryMethod]
                    public void RegisterSetting<T>(Identification id, SettingDefinition<T> definition) { }
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync(token);

        // SPARK0213 = GenericValueMismatch. The wrapper-over-T shape is valid and must not trip it.
        await Assert.That(diagnostics.Any(d => d.Id == "SPARK0213"))
            .IsFalse()
            .Because("Wrapper-over-T register method must not report GenericValueMismatch (SPARK0213)");
    }
}
