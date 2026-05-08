using System.Linq;
using System.Threading.Tasks;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

public sealed class KeyedFactoryGenerationMarkerAnalyzerTests : AnalyzerTestBase<KeyedFactoryGenerationMarkerAnalyzer>
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
    public async Task SupportedDiagnostics_ContainsExactlySpark0260And0261()
    {
        var analyzer = new KeyedFactoryGenerationMarkerAnalyzer();
        var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).OrderBy(x => x).ToArray();
        await Assert.That(ids).IsEquivalentTo(new[] { "SPARK0260", "SPARK0261" });
    }

    [Test]
    public async Task MarkerOnNonRegistryMethod_Reports_SPARK0260()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyClass
        {
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void NotARegistryMethod<T>(Identification id) { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0260", 1);
    }

    [Test]
    public async Task MarkerOnValueRegistration_Reports_SPARK0260()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterValue(Identification id, string v) { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0260", 1);
    }

    [Test]
    public async Task MarkerOnGenericValueRegistration_Reports_SPARK0260()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterGenericValue<T>(Identification id, T v) { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0260", 1);
    }

    [Test]
    public async Task MarkerOnTypeRegistration_HappyPath_NoDiagnostic()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo : IHasIdentification { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : class, IFoo, IHasIdentification { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task MissingClassConstraint_Reports_SPARK0261()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : IFoo, IHasIdentification { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0261", 1);
    }

    [Test]
    public async Task MissingTBaseConstraint_Reports_SPARK0261()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : class, IHasIdentification { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0261", 1);
    }

    [Test]
    public async Task MissingIHasIdentificationConstraint_Reports_SPARK0261()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : class, IFoo { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0261", 1);
    }

    [Test]
    public async Task ExtraUnrelatedConstraint_NoDiagnostics()
    {
        // Adding an additional unrelated constraint (IBar) alongside the literal
        // TBase (IFoo) and IHasIdentification is a perfectly valid use of the marker.
        // SPARK0261 already guarantees TBase is present; no further hierarchy
        // requirement applies, so no diagnostics from this analyzer.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }
        public interface IBar { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : class, IFoo, IBar, IHasIdentification { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task CanonicalShape_NoDiagnostics()
    {
        // Regression for K0262 false positive: the canonical keyed-factory marker
        // shape used by samples/MinimalSampleMod (DummyRegistry.RegisterProvider).
        // TBase (IFoo) is literally a constraint and IHasIdentification is required
        // by SPARK0261. IFoo does NOT extend IHasIdentification — the previous K0262
        // implementation incorrectly flagged this shape. No diagnostics expected.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IFoo { }

        public class MyRegistry
        {
            [RegistryMethod]
            [KeyedFactoryGenerationMarkerAttribute<IFoo>]
            public void RegisterFoo<T>(Identification id)
                where T : class, IFoo, IHasIdentification { }
        }
        """;

        TestSources.Add(("P.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }
}
