using System.Threading.Tasks;
using Sparkitect.Generator.GameState.Analyzers;

namespace Sparkitect.Generator.Tests.GameState;

public class StateServiceAnalyzerTests : AnalyzerTestBase<StateServiceAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.StateServiceTypes);
    }

    // SPARK0301 tests
    [Test]
    public async Task InterfaceNotImplemented_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;

            public interface IMyFacade { }

            [StateFacade<IMyFacade>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService { }  // Missing : IMyService
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0301", 1);
    }

    [Test]
    public async Task InterfaceImplemented_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            [StateFacade<IMyFacade>]
            public interface IMyService { }
            public interface IMyFacade { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // SPARK0302 tests
    [Test]
    public async Task FacadeNotImplemented_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;

            [StateFacade<IMyFacade>]
            public interface IMyService { }
            public interface IMyFacade { }

            [StateService<IMyService>]
            public class MyService : IMyService { }  // Missing : IMyFacade
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0302", 1);
    }

    [Test]
    public async Task AllFacadesImplemented_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            [StateFacade<IFacade1>]
            [StateFacade<IFacade2>]
            public interface IMyService { }
            public interface IFacade1 { }
            public interface IFacade2 { }

            [StateService<IMyService>]
            public class MyService : IMyService, IFacade1, IFacade2 { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // SPARK0303 tests
    [Test]
    public async Task InterfaceMissingFacade_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;

            public interface IMyService { }  // Missing [StateFacade<T>]

            [StateService<IMyService>]
            public class MyService : IMyService { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0303", 1);
    }

    [Test]
    public async Task InterfaceHasFacade_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            [StateFacade<IMyFacade>]
            public interface IMyService { }
            public interface IMyFacade { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // Edge case
    [Test]
    public async Task ClassWithoutStateServiceAttribute_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            public interface IMyService { }
            public class MyService : IMyService { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }
}
