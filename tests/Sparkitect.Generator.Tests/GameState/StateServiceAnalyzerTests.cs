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
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
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
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
            public interface IMyFacade { }

            [StateFacade<IMyFacade>]
            public interface IMyService { }

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
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
            public interface IMyFacade { }

            [StateFacade<IMyFacade>]
            public interface IMyService { }

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
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
            public interface IFacade1 { }
            [FacadeFor<IMyService>]
            public interface IFacade2 { }

            [StateFacade<IFacade1>]
            [StateFacade<IFacade2>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService, IFacade1, IFacade2 { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // SPARK0303 removed -- facades are optional
    [Test]
    public async Task InterfaceWithoutFacades_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task InterfaceHasFacade_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
            public interface IMyFacade { }

            [StateFacade<IMyFacade>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // SPARK0304 tests
    [Test]
    public async Task FacadeMissingFacadeFor_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.DI.GeneratorAttributes;

            public interface IMyFacade { }  // Missing [FacadeFor<IMyService>]

            [StateFacade<IMyFacade>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0304", 1);
    }

    [Test]
    public async Task FacadeWithFacadeFor_NoDiagnostic_SPARK0304()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.DI.GeneratorAttributes;

            [FacadeFor<IMyService>]
            public interface IMyFacade { }

            [StateFacade<IMyFacade>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // SPARK0305 tests
    [Test]
    public async Task FacadeForPointingToWrongService_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.DI.GeneratorAttributes;

            public interface IOtherService { }

            [FacadeFor<IOtherService>]
            public interface IMyFacade { }  // Points to IOtherService, but declared on IMyService

            [StateFacade<IMyFacade>]
            public interface IMyService { }

            [StateService<IMyService>]
            public class MyService : IMyService, IMyFacade { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        // Both SPARK0304 (no FacadeFor<IMyService> found) and SPARK0305 (FacadeFor<IOtherService> inconsistent)
        await AssertDiagnosticCount(diagnostics, "SPARK0304", 1);
        await AssertDiagnosticCount(diagnostics, "SPARK0305", 1);
    }

    [Test]
    public async Task SelfReferencingStateFacade_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.DI.GeneratorAttributes;

            [StateFacade<IMyService>]
            public interface IMyService { }  // Self-referencing: [StateFacade<IMyService>] on IMyService itself

            [StateService<IMyService>]
            public class MyService : IMyService { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        // Self-referencing means IMyService (as facade) lacks [FacadeFor<IMyService>] -> SPARK0304
        await AssertDiagnosticCount(diagnostics, "SPARK0304", 1);
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
