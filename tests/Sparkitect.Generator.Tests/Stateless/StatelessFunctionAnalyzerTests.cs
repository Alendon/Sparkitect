using System.Linq;
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

    // SPARK0401 - Method must be static

    [Test]
    public async Task NonStaticMethod_WithStatelessAttribute_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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


    // SPARK0402 - Multiple scheduling attributes

    [Test]
    public async Task MultipleSchedulingAttributes_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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


    // SPARK0403 - Parameter not DI-resolvable

    [Test]
    public async Task ConcreteParameter_ReportsWarning()
    {
        var code = """
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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

    [Test]
    public async Task ConcreteParameterWithAllowConcreteResolution_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using Sparkitect.DI.GeneratorAttributes;

            [AllowConcreteResolution]
            public class QueryType { }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod(QueryType query) { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }


    // SPARK0404 - Missing IHasIdentification

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
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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


    // SPARK0405 - Orphan ordering attributes

    [Test]
    public async Task OrderBeforeWithoutScheduling_ReportsWarning()
    {
        var code = """
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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


    // SPARK0406 - Non-public static access

    [Test]
    public async Task PrivateStaticField_Read_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static int _x;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = _x; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }

    [Test]
    public async Task PrivateStaticField_Write_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static int _x;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { _x = 42; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }

    [Test]
    public async Task InternalStaticProperty_Read_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                internal static int X { get; set; }

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }

    [Test]
    public async Task ConstField_FromStateless_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private const int X = 5;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 0);
    }

    [Test]
    public async Task PublicStaticField_FromStateless_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                public static int X;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 0);
    }

    [Test]
    public async Task ProtectedStaticField_FromStateless_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class BaseOwner
            {
                protected static int X;
            }

            public class TestOwner : BaseOwner, IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 0);
    }

    [Test]
    public async Task PrivateStaticField_FromNonStatelessMethod_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static int _x;

                public static void RegularMethod() { var y = _x; _x = 42; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 0);
    }

    [Test]
    public async Task RenderGraphShape_PrivateStaticNullableRefField_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class FakeRg { public void Frame() { } public void Dispose() { } public static FakeRg Create() => new(); }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static FakeRg? _renderGraph;

                [TestFunction("create")]
                [TestScheduling]
                public static void Create() { _renderGraph = FakeRg.Create(); }

                [TestFunction("frame")]
                [TestScheduling]
                public static void Frame() { _renderGraph?.Frame(); }

                [TestFunction("destroy")]
                [TestScheduling]
                public static void Destroy() { _renderGraph?.Dispose(); _renderGraph = null; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        // Expected baseline is 4 distinct reference sites: Create assignment-write,
        // Frame null-conditional read, Destroy null-conditional read, Destroy assignment-write.
        // Per plan, accept any count >= 3; pin to observed if Roslyn lowers null-conditional differently.
        var observed = diagnostics.Count(d => d.Id == "SPARK0406");
        await Assert.That(observed).IsGreaterThanOrEqualTo(3)
            .Because($"Expected at least 3 SPARK0406 reports for the _renderGraph shape, observed {observed}");
        await AssertDiagnosticCount(diagnostics, "SPARK0406", 4);
    }

    [Test]
    public async Task PrivateProtectedStaticField_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class BaseOwner
            {
                private protected static int X;
            }

            public class TestOwner : BaseOwner, IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }

    [Test]
    public async Task FileStaticField_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            file class FileLocal { internal static int X; }

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = FileLocal.X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }

    [Test]
    public async Task StaticMethodCall_FromStateless_NoDiagnostic()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static int Compute() => 1;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { Compute(); }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 0);
    }

    [Test]
    public async Task StaticReadonlyField_NonPublic_FromStateless_ReportsError()
    {
        var code = """
            #pragma warning disable SPARK0262
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class TestOwner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                private static readonly int X = 5;

                [TestFunction("my_func")]
                [TestScheduling]
                public static void MyMethod() { var y = X; }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0406", 1);
    }


    // Edge cases

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
            #pragma warning disable SPARK0262
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
            #pragma warning disable SPARK0262
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

}
