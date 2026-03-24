using System.Threading.Tasks;
using Sparkitect.Generator.Stateless.Analyzers;

namespace Sparkitect.Generator.Tests.Stateless;

public class StatelessFunctionAnalyzerTests : AnalyzerTestBase<StatelessFunctionAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.MetadataTypes);
        TestSources.Add(TestData.StatelessCoreTypes);
        TestSources.Add(TestData.StatelessTestTypes);
    }

    #region SPARK0401 - Method must be static

    [Test]
    public async Task NonStaticMethod_WithStatelessAttribute_ReportsError()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0401", 1);
    }

    [Test]
    public async Task StaticMethod_WithStatelessAttribute_NoDiagnostic_SPARK0401()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region SPARK0402 - Multiple scheduling attributes

    [Test]
    public async Task MultipleSchedulingAttributes_ReportsError()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0402", 1);
    }

    [Test]
    public async Task SingleSchedulingAttribute_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region SPARK0403 - Parameter not DI-resolvable

    [Test]
    public async Task ConcreteParameter_ReportsWarning()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class ConcreteDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(ConcreteDependency dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0403", 1);
    }

    [Test]
    public async Task InterfaceParameter_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public interface IDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(IDependency dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task NullableConcreteParameter_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class ConcreteDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(ConcreteDependency? dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task AbstractParameter_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public abstract class AbstractDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(AbstractDependency dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region SPARK0404 - Missing IHasIdentification

    [Test]
    public async Task TypeWithoutIHasIdentification_ReportsError()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner
            {
                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0404", 1);
    }

    [Test]
    public async Task TypeWithIHasIdentification_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task TypeWithoutIHasIdentification_WithParentIdAttribute_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class ParentOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;
            }

            public class TestOwner  // Does NOT implement IHasIdentification
            {
                [TestFunction("my_func")]
                [TestScheduling]
                [ParentId<ParentOwner>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region SPARK0405 - Orphan ordering attributes

    [Test]
    public async Task OrderBeforeWithoutScheduling_ReportsWarning()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class OtherFunction : IStatelessFunction, IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                public void Execute() { }
                public void Initialize(object container, IReadOnlyDictionary<Type, Type> facadeMap) { }
                Identification IStatelessFunction.Identification => Identification;
            }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [OrderBefore<OtherFunction>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0405", 1);
    }

    [Test]
    public async Task OrderAfterWithoutScheduling_ReportsWarning()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class OtherFunction : IStatelessFunction, IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                public void Execute() { }
                public void Initialize(object container, IReadOnlyDictionary<Type, Type> facadeMap) { }
                Identification IStatelessFunction.Identification => Identification;
            }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [OrderAfter<OtherFunction>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0405", 1);
    }

    [Test]
    public async Task OrderAttributesWithScheduling_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class OtherFunction : IStatelessFunction, IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                public void Execute() { }
                public void Initialize(object container, IReadOnlyDictionary<Type, Type> facadeMap) { }
                Identification IStatelessFunction.Identification => Identification;
            }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                [OrderBefore<OtherFunction>]
                [OrderAfter<OtherFunction>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region Edge cases

    [Test]
    public async Task MethodWithoutStatelessAttribute_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner
            {
                // No [StatelessFunctionAttribute] - should not trigger any diagnostics
                public void RegularMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ValidStatelessFunction_NoDiagnostic()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public interface IDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(IDependency dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task MultipleParameters_OnlyConcreteReportsWarning()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public interface IDependency { }
            public class ConcreteDependency { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(IDependency dep1, ConcreteDependency dep2, ConcreteDependency? dep3) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        // Only dep2 should report - dep1 is interface, dep3 is nullable
        await AssertDiagnosticCount(diagnostics, "SPARK0403", 1);
    }

    [Test]
    public async Task MultipleIssues_ReportsAll()
    {
        var code = """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class ConcreteDependency { }

            public class TestOwner
            {
                [TestFunction("my_func")]
                [TestScheduling]
                [TestScheduling]
                public void MyMethod(ConcreteDependency dep) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        // Should have:
        // - SPARK0401 (not static)
        // - SPARK0402 (multiple scheduling)
        // - SPARK0403 (concrete param)
        // - SPARK0404 (missing IHasIdentification)
        await AssertDiagnosticCount(diagnostics, "SPARK0401", 1);
        await AssertDiagnosticCount(diagnostics, "SPARK0402", 1);
        await AssertDiagnosticCount(diagnostics, "SPARK0403", 1);
        await AssertDiagnosticCount(diagnostics, "SPARK0404", 1);
    }

    #endregion
}
