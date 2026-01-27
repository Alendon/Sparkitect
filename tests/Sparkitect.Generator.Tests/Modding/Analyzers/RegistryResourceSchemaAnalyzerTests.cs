using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

public sealed class RegistryResourceSchemaAnalyzerTests : AnalyzerTestBase<RegistryResourceSchemaAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.ModdingCode);
        TestSources.Add(TestData.Sparkitect);
    }

    [Test]
    public async Task Smoke_NoDiagnostics_OnEmpty()
    {
        TestSources.Add(("Empty.cs", "namespace N { class C { } }"));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task SupportedDiagnostics_ContainsExpected()
    {
        var analyzer = new RegistryResourceSchemaAnalyzer();
        var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).ToArray();
        await Assert.That(ids.Contains("SPARK0242")).IsTrue();
        await Assert.That(ids.Contains("SPARK0245")).IsTrue();
    }

    [Test]
    public async Task Yaml_DuplicateIds_PerRegistry_Reports_2045()
    {
        var yaml = """
        MinimalSampleMod.DummyRegistry.Register:
          - dup: "file1.txt"
          - dup: "file2.txt"
        Other.Registry.Register:
          - x: "a.txt"
          - x: "b.txt"
        """;

        AdditionalFiles.Add(("/data/sample3.sparkres.yaml", yaml));
        TestSources.Add(("Empty.cs", "namespace N { class C { } }"));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0245", 2);
    }

    [Test]
    public async Task Yaml_UnknownRegistryOrMethod_Reports_2042()
    {
        var yaml = """
        NoSuch.Registry.Register:
          - x: "file.txt"
        DiTest.DummyRegistry.DoesNotExist:
          - y: "file.txt"
        """;

        var regCode = """
        using Sparkitect.Modding;

        namespace DiTest;
        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        """;

        AdditionalFiles.Add(("/data/y_unknown.sparkres.yaml", yaml));
        TestSources.Add(("R.cs", regCode));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0242", 2);
    }

    [Test]
    public async Task Yaml_UnknownFileKey_Reports_2043()
    {
        var yaml = """
        DiTest.DummyRegistry.RegisterValue:
          - x:
              wrong: a.txt
        """;

        var regCode = """
        using Sparkitect.Modding;

        namespace DiTest;
        [Registry(Identifier = "dummy")]
        [UseResourceFile(Key = "asset", Required = true)]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        """;

        AdditionalFiles.Add(("/data/y_unknownkey.sparkres.yaml", yaml));
        TestSources.Add(("R2.cs", regCode));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0243", 1);
    }

    [Test]
    public async Task Yaml_MissingRequiredKey_Reports_2044()
    {
        var yaml = """
        DiTest.DummyRegistry.RegisterValue:
          - x:
              data: data.bin
        """;

        var regCode = """
        using Sparkitect.Modding;

        namespace DiTest;
        [Registry(Identifier = "dummy")]
        [UseResourceFile(Key = "config", Required = true)]
        [UseResourceFile(Key = "data", Required = false)]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        """;

        AdditionalFiles.Add(("/data/y_missingreq.sparkres.yaml", yaml));
        TestSources.Add(("R3.cs", regCode));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0244", 1);
    }
}
