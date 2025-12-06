using System.Threading.Tasks;
using Sparkitect.Generator.GameState.Analyzers;

namespace Sparkitect.Generator.Tests.GameState.Analyzers;

public class StateFunctionAnalyzerTests : AnalyzerTestBase<StateFunctionAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GameStateAttributes);
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
    }

    [Test]
    public async Task StateFunctionMissingSchedule_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3001", 1);
    }

    [Test]
    public async Task StateFunctionMultipleSchedules_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                [OnFrameEnter]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3002", 1);
    }

    [Test]
    public async Task StateFunctionNotStatic_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                public void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3003", 1);
    }

    [Test]
    public async Task StateFunctionDuplicateKey_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("duplicate")]
                [PerFrame]
                public static void Method1() { }

                [StateFunction("duplicate")]
                [OnFrameEnter]
                public static void Method2() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3004", 1);
    }

    [Test]
    public async Task StateFunctionParameterNotAbstract_ReportsWarning()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public interface IService { }
            public class ConcreteService : IService { }

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                public static void TestMethod(ConcreteService service) { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3005", 1);
    }

    [Test]
    public async Task StateFunctionParameterAbstract_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public interface IService { }

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                public static void TestMethod(IService service) { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task OrderingInvalidTargetType_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class NotAModule { }

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                [OrderAfter<NotAModule>("some_key")]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3008", 1);
    }

    [Test]
    public async Task OrderingValidTargetType_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class OtherModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];
                
                public const string SomeKey = "some_key";
            }

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test")]
                [PerFrame]
                [OrderAfter<OtherModule>(OtherModule.SomeKey)]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task StateFunctionInvalidKey_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("")]
                [PerFrame]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3010", 1);
    }

    [Test]
    public async Task StateFunctionNotInModule_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;

            public class NotAModule
            {
                [StateFunction("test")]
                [PerFrame]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("NotAModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3011", 1);
    }

    [Test]
    public async Task ValidStateFunction_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public interface IService { }

            public class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Empty;
                public static Span<Identification> RequiredModules => [];

                [StateFunction("valid_function")]
                [PerFrame]
                public static void ValidFunction(IService service) { }
            }
            """;

        TestSources.Add(("TestModule.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task StateFunctionInDescriptor_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public interface IService { }

            public class TestDescriptor : IStateDescriptor
            {
                public static Identification ParentId => Identification.Empty;
                public static Identification Identification => Identification.Empty;
                public static IReadOnlyList<Identification> Modules => [];

                [StateFunction("descriptor_function")]
                [OnFrameEnter]
                public static void DescriptorFunction(IService service) { }
            }
            """;

        TestSources.Add(("TestDescriptor.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task StateFunctionNotInModuleOrDescriptor_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;

            public class NotAModuleOrDescriptor
            {
                [StateFunction("test")]
                [PerFrame]
                public static void TestMethod() { }
            }
            """;

        TestSources.Add(("NotValid.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3011", 1);
    }
}