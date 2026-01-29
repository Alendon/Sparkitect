using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

public sealed class RegistryProviderUsageAnalyzerTests : AnalyzerTestBase<RegistryProviderUsageAnalyzer>
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
        var analyzer = new RegistryProviderUsageAnalyzer();
        var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).ToArray();
        await Assert.That(ids.Contains("SPARK0220")).IsTrue();
    }

    [Test]
    public async Task MissingId_Reports_2020()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue()] // id missing
            public static string NoId() => "x";
        }
        """;

        TestSources.Add(("P1.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0220", 1);
    }

    [Test]
    public async Task NonStaticProviderMethod_Reports_2021()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public class Providers
        {
            [DummyRegistry.RegisterValue("ok")]
            public string Value() => "x"; // non-static
        }
        """;

        TestSources.Add(("P2.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0221", 1);
    }

    [Test]
    public async Task UnknownRegistry_Reports_2022()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace DiTest;
        
        // Define a valid registry so attribute pattern is recognizable
        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }
        
        public static class Providers
        {
            [MissingRegistry.RegisterValue("ok")] // Unknown registry type
            public static string Value() => "x";
        }
        """;

        TestSources.Add(("P3.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0222", 1);
    }

    [Test]
    public async Task UnknownMethod_Reports_2023()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.DoesNotExist("ok")]
            public static string Value() => "x";
        }
        """;

        TestSources.Add(("P4.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0223", 1);
    }

    [Test]
    public async Task ValidSnakeCase_NoUnrelatedDiagnostics()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue("id_123")] // valid snake_case
            public static string Value() => "x";
        }
        """;

        TestSources.Add(("P5b.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task KindMismatch_TypeProvider_ToValueMethod_Reports_2024()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        [DummyRegistry.RegisterValue("ok")]
        public class Provided { }
        """;

        TestSources.Add(("P6.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0224", 1);
    }

    [Test]
    public async Task IncompatibleReturnType_Reports_2025()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue("ok")]
            public static int Value() => 42; // expected string
        }
        """;

        TestSources.Add(("P7.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0225", 1);
    }

    [Test]
    public async Task GenericConstraintViolation_Reports_2026()
    {
        var code = """
        using Sparkitect.Modding;
        using System;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterType<T>(Identification id) where T : IDisposable { }
        }

        [DummyRegistry.RegisterType("ok")]
        public class Provided { } // does not implement IDisposable
        """;

        TestSources.Add(("P8.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0226", 1);
    }

    [Test]
    public async Task DiParameterGuidance_Reports_2032()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public class Concrete { }

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue("ok")]
            public static string Value(Concrete dep) => "x"; // Non-abstract and not nullable
        }
        """;

        TestSources.Add(("P9.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0232", 1);
    }

    [Test]
    public async Task DuplicateIdsWithinRegistry_Reports_2030()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue("dup")]
            public static string A() => "x";

            [DummyRegistry.RegisterValue("dup")]
            public static string B() => "y";
        }
        """;

        TestSources.Add(("P10.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0230", 1);
    }

    [Test]
    public async Task DuplicateNormalizedPropertyNames_Reports_2050()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [Registry(Identifier = "dummy")]
        public class DummyRegistry : IRegistry
        {
            [RegistryMethod]
            public void RegisterValue(Identification id, string value) { }
        }

        public static class Providers
        {
            [DummyRegistry.RegisterValue("some_id")]
            public static string A() => "x";

            [DummyRegistry.RegisterValue("some__id")]
            public static string B() => "y";
        }
        """;

        TestSources.Add(("P11.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0250", 1);
    }
}
