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
        await Assert.That(ids.Contains("SPARK2041")).IsTrue();
    }

    [Test]
    public async Task Yaml_MissingId_Reports_2040()
    {
        // Missing 'id' field
        var yaml = """
        MinimalSampleMod.DummyRegistry.Register:
          - file: a.txt
        """;

        AdditionalFiles.Add(("/data/sample.sparkres.yaml", yaml));
        TestSources.Add(("Empty.cs", "namespace N { class C { } }"));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK2040", 1);
    }

    [Test]
    public async Task Yaml_FileAndFiles_Reports_2041()
    {
        // Both 'file' and 'files' present
        var yaml = """
        MinimalSampleMod.DummyRegistry.Register:
          - id: hello
            file: a.txt
            files:
              config: b.json
        """;

        AdditionalFiles.Add(("/data/sample2.sparkres.yaml", yaml));
        TestSources.Add(("Empty.cs", "namespace N { class C { } }"));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK2041", 1);
    }

    [Test]
    public async Task Yaml_DuplicateIds_PerRegistry_Reports_2045()
    {
        var yaml = """
        MinimalSampleMod.DummyRegistry.Register:
          - id: dup
          - id: dup
        Other.Registry.Register:
          - id: x
          - id: x
        """;

        AdditionalFiles.Add(("/data/sample3.sparkres.yaml", yaml));
        TestSources.Add(("Empty.cs", "namespace N { class C { } }"));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK2045", 2);
    }

    [Test]
    public async Task Yaml_UnknownRegistryOrMethod_Reports_2042()
    {
        var yaml = """
        NoSuch.Registry.Register:
          - id: x
        DiTest.DummyRegistry.DoesNotExist:
          - id: y
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
        await AssertDiagnosticCount(diagnostics, "SPARK2042", 2);
    }

    [Test]
    public async Task Yaml_UnknownFileKey_Reports_2043()
    {
        var yaml = """
        DiTest.DummyRegistry.RegisterValue:
          - id: x
            files:
              wrong: a.txt
        """;

        var regCode = """
        using Sparkitect.Modding;

        namespace DiTest;
        [Registry(Identifier = "dummy")]
        [UseResourceFile(Identifier = "asset", Required = true)]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        """;

        AdditionalFiles.Add(("/data/y_unknownkey.sparkres.yaml", yaml));
        TestSources.Add(("R2.cs", regCode));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK2043", 1);
    }

    [Test]
    public async Task Yaml_MissingRequiredKey_Reports_2044()
    {
        var yaml = """
        DiTest.DummyRegistry.RegisterValue:
          - id: x
            files:
              data: data.bin
        """;

        var regCode = """
        using Sparkitect.Modding;

        namespace DiTest;
        [Registry(Identifier = "dummy")]
        [UseResourceFile(Identifier = "config", Required = true)]
        [UseResourceFile(Identifier = "data", Required = false)]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        """;

        AdditionalFiles.Add(("/data/y_missingreq.sparkres.yaml", yaml));
        TestSources.Add(("R3.cs", regCode));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK2044", 1);
    }
}
