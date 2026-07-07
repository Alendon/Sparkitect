using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

public sealed class RegistryShapeAnalyzerTests : AnalyzerTestBase<RegistryShapeAnalyzer>
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
        var analyzer = new RegistryShapeAnalyzer();
        var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).ToArray();
        await Assert.That(ids.Contains("SPARK0201")).IsTrue();
        await Assert.That(ids.Contains("SPARK0206")).IsTrue();
    }

    [Test]
    public async Task RegistryRequiresInterface_WhenMissing_Reports_2001()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N;
        
        [Registry(Identifier = "valid_id")]
        public class BadRegistry { }
        """;

        TestSources.Add(("R1.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0201", 1);
    }

    [Test]
    public async Task MissingIdentifier_Reports_2002()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N;
        
        [Registry]
        public class MyRegistry : IRegistry<TestModule> { }
        """;

        TestSources.Add(("R2.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0202", 1);
    }

    [Test]
    public async Task GlobalNamespace_Reports_2003()
    {
        var code = """
        using Sparkitect.Modding;
        
        [Registry(Identifier = "valid_id")]
        public class GlobalRegistry : IRegistry<TestModule> { }
        """;

        TestSources.Add(("R3.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0203", 1);
    }

    [Test]
    public async Task NestedType_Reports_2003()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N
        {
            public class Outer
            {
                [Registry(Identifier = "valid_id")]
                public class Inner : IRegistry<TestModule> { }
            }
        }
        """;

        TestSources.Add(("R4.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0203", 1);
    }

    [Test]
    public async Task Identifier_NotSnakeCase_Reports_2006()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N;
        
        [Registry(Identifier = "bad-id")]
        public class MyRegistry : IRegistry<TestModule> { }
        """;

        TestSources.Add(("R5.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0206", 1);
    }

    [Test]
    public async Task Identifier_SnakeCase_No_2006()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N;
        
        [Registry(Identifier = "good_id")]
        public class MyRegistry : IRegistry<TestModule> { }
        """;

        TestSources.Add(("R6.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await Assert.That(diagnostics.Any(d => d.Id == "SPARK0206")).IsFalse();
    }

    [Test]
    public async Task Identifier_SnakeCase_WithNumbers_No_2006()
    {
        var code = """
        using Sparkitect.Modding;
        
        namespace N;
        
        [Registry(Identifier = "good_id_123")]
        public class MyRegistry : IRegistry<TestModule> { }
        """;

        TestSources.Add(("R6b.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await Assert.That(diagnostics.Any(d => d.Id == "SPARK0206")).IsFalse();
    }

    [Test]
    public async Task RegistryMethodOutsideRegistry_Reports_2010()
    {
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; }

        namespace N;
        
        public class Host
        {
            [Sparkitect.Modding.RegistryMethod]
            public void R(Sparkitect.Modding.Identification id) { }
        }
        """;

        TestSources.Add(("M1.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0210", 1);
    }

    [Test]
    public async Task RegistryMethod_MultipleTypeParams_NoLongerReports_2012()
    {
        // D-02: the arity cap is lifted — a type-source register method with two type parameters,
        // where T1 is constrained by RelationShip<T2>, compiles clean at the gate level (no SPARK0212,
        // no other registry-shape diagnostic). Mirrors the load-bearing constraint-walk shape from
        // CONTEXT.md D-03 (Reg<T1,T2>() where T1 : RelationShip<T2>).
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }

        namespace N
        {
            public interface RelationShip<T> { }

            public class A { }
            public class B : RelationShip<A> { }

            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry<Sparkitect.Modding.TestModule>
            {
                [Sparkitect.Modding.RegistryMethod]
                public void Reg<T1, T2>(Sparkitect.Modding.Identification id) where T1 : RelationShip<T2> { }
            }
        }
        """;

        TestSources.Add(("M2.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task RegistryMethod_MultiTypeParam_ValueReferencesNone_Reports_2013()
    {
        // The mismatch gate is preserved under the lifted cap: a two-parameter value-source method
        // with two type parameters, where the value parameter references neither, still reports
        // GenericValueMismatch — looping all type parameters must not accidentally suppress this.
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }

        namespace N
        {
            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry
            {
                [Sparkitect.Modding.RegistryMethod]
                public void M<T1, T2>(Sparkitect.Modding.Identification id, string value) { }
            }
        }
        """;

        TestSources.Add(("M2b.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0213", 1);
    }

    [Test]
    public async Task RegistryMethod_FirstParamNotIdentification_Reports_2014()
    {
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }
        
        namespace N
        {
            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry
            {
                [Sparkitect.Modding.RegistryMethod]
                public void M(string badFirstParam) { }
            }
        }
        """;

        TestSources.Add(("M3.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0214", 1);
    }

    [Test]
    public async Task RegistryMethod_GenericValueMismatch_Reports_2013()
    {
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }
        
        namespace N
        {
            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry
            {
                [Sparkitect.Modding.RegistryMethod]
                public void M<T>(Sparkitect.Modding.Identification id, string value) { }
            }
        }
        """;

        TestSources.Add(("M4.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0213", 1);
    }

    [Test]
    public async Task RegistryMethod_InvalidParamCount_Reports_2011()
    {
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }
        
        namespace N
        {
            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry
            {
                [Sparkitect.Modding.RegistryMethod]
                public void M() { }
            }
        }
        """;

        TestSources.Add(("M5.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0211", 1);
    }

    [Test]
    public async Task DuplicateMethodNames_Reports_2015()
    {
        var code = """
        namespace Sparkitect.Modding { public class RegistryMethodAttribute : System.Attribute; public interface IRegistry; }
        
        namespace N
        {
            [Sparkitect.Modding.Registry(Identifier = "good_id")]
            public class R : Sparkitect.Modding.IRegistry
            {
                [Sparkitect.Modding.RegistryMethod]
                public void M(Sparkitect.Modding.Identification id) { }
                
                [Sparkitect.Modding.RegistryMethod]
                public void M(Sparkitect.Modding.Identification id, string value) { }
            }
        }
        """;

        TestSources.Add(("M6.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        // Duplicate should report for the second occurrence only
        await AssertDiagnosticCount(diagnostics, "SPARK0215", 1);
    }
}
