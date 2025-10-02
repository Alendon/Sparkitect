using System.Threading.Tasks;
using Sparkitect.Generator.GameState.Analyzers;

namespace Sparkitect.Generator.Tests.GameState.Analyzers;

public class StateOrderingAnalyzerTests : AnalyzerTestBase<StateOrderingAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GameStateAttributes);
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
    }

    [Test]
    public async Task ModuleOrderingCycle_TwoModules_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [OrderModuleAfter<ModuleB>]
            public class ModuleA : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleA>]
            public class ModuleB : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static IReadOnlyList<Type> UsedServices => [];
            }
            """;

        TestSources.Add(("Modules.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3009", 1);
    }

    [Test]
    public async Task ModuleOrderingCycle_ThreeModules_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [OrderModuleAfter<ModuleC>]
            public class ModuleA : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleA>]
            public class ModuleB : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleB>]
            public class ModuleC : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 3);
                public static IReadOnlyList<Type> UsedServices => [];
            }
            """;

        TestSources.Add(("Modules.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK3009", 1);
    }

    [Test]
    public async Task ModuleOrderingValid_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class ModuleA : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleA>]
            public class ModuleB : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleB>]
            public class ModuleC : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 3);
                public static IReadOnlyList<Type> UsedServices => [];
            }
            """;

        TestSources.Add(("Modules.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ModuleOrderingMixed_BeforeAndAfter_Valid_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class ModuleA : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            [OrderModuleAfter<ModuleA>]
            [OrderModuleBefore<ModuleC>]
            public class ModuleB : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static IReadOnlyList<Type> UsedServices => [];
            }

            public class ModuleC : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 3);
                public static IReadOnlyList<Type> UsedServices => [];
            }
            """;

        TestSources.Add(("Modules.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task NoModules_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;

            public class SomeClass
            {
            }
            """;

        TestSources.Add(("SomeClass.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertNoDiagnostics(diagnostics);
    }
}