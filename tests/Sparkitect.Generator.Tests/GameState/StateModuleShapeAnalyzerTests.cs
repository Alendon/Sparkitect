using System.Threading.Tasks;
using Sparkitect.Generator.GameState.Analyzers;

namespace Sparkitect.Generator.Tests.GameState;

public class StateModuleShapeAnalyzerTests : AnalyzerTestBase<StateModuleShapeAnalyzer>
{
    // Minimal stubs for the composition surface so the harness compiles the fixtures
    // without the engine assembly: the capability interfaces, the transitive authoring bases, and
    // the generated nested registration attributes (applied as [ModuleRegistry.RegisterModule("k")]).
    private const string CompositionStubs = """
        namespace Sparkitect.Modding
        {
            public interface IHasIdentification { }
            public interface IRegisterMarker { }
            public readonly struct Identification { }
        }

        namespace Sparkitect.GameState
        {
            public interface IStateModule
            {
                IReadOnlyList<Sparkitect.Modding.Identification> Requires { get; }
                IReadOnlyList<Sparkitect.Modding.Identification> ActivatesWith { get; }
            }

            public interface IGameState
            {
                Sparkitect.Modding.Identification ParentId { get; }
                IReadOnlyList<Sparkitect.Modding.Identification> DirectModules { get; }
            }

            public abstract class TransitiveStateModule : IStateModule
            {
                public abstract IReadOnlyList<Sparkitect.Modding.Identification> Requires { get; }
                public virtual IReadOnlyList<Sparkitect.Modding.Identification> ActivatesWith =>
                    new List<Sparkitect.Modding.Identification>();
            }

            public abstract class TransitiveGameState : IGameState
            {
                public abstract Sparkitect.Modding.Identification ParentId { get; }
                public abstract IReadOnlyList<Sparkitect.Modding.Identification> DirectModules { get; }
            }

            public partial class ModuleRegistry
            {
                [AttributeUsage(AttributeTargets.Class)]
                public sealed class RegisterModuleAttribute : Attribute, Sparkitect.Modding.IRegisterMarker
                {
                    public RegisterModuleAttribute(string identifier) { }
                }
            }

            public partial class StateRegistry
            {
                [AttributeUsage(AttributeTargets.Class)]
                public sealed class RegisterStateAttribute : Attribute, Sparkitect.Modding.IRegisterMarker
                {
                    public RegisterStateAttribute(string identifier) { }
                }
            }
        }
        """;

    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(("CompositionStubs.cs", CompositionStubs));
    }

    // (1) Module deriving the transitive base and declared partial -> no diagnostic.
    [Test]
    public async Task ModuleDerivingTransitiveBase_Partial_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [ModuleRegistry.RegisterModule("good")]
            public partial class GoodModule : TransitiveStateModule, IHasIdentification
            {
                public override IReadOnlyList<Identification> Requires => new List<Identification>();
            }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // (2) Module implementing IStateModule directly (escape hatch) and partial -> no diagnostic.
    [Test]
    public async Task ModuleImplementingInterfaceDirectly_Partial_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [ModuleRegistry.RegisterModule("escape")]
            public partial class EscapeModule : IStateModule, IHasIdentification
            {
                public IReadOnlyList<Identification> Requires => new List<Identification>();
                public IReadOnlyList<Identification> ActivatesWith => new List<Identification>();
            }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // (3) Module deriving neither base nor implementing the interface -> SPARK0306.
    [Test]
    public async Task ModuleWithNeitherBaseNorInterface_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [ModuleRegistry.RegisterModule("bad")]
            public partial class BadModule : IHasIdentification { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0306", 1);
    }

    // (4) Correctly-derived but non-partial registered type -> SPARK0306.
    [Test]
    public async Task ModuleDerivingBase_NonPartial_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [ModuleRegistry.RegisterModule("nonpartial")]
            public class NonPartialModule : TransitiveStateModule, IHasIdentification
            {
                public override IReadOnlyList<Identification> Requires => new List<Identification>();
            }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0306", 1);
    }

    // (5a) State deriving TransitiveGameState and partial -> no diagnostic.
    [Test]
    public async Task StateDerivingTransitiveBase_Partial_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [StateRegistry.RegisterState("good")]
            public partial class GoodState : TransitiveGameState, IHasIdentification
            {
                public override Identification ParentId => default;
                public override IReadOnlyList<Identification> DirectModules => new List<Identification>();
            }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    // (5b) State analog that is mis-shaped (neither TransitiveGameState nor IGameState) -> SPARK0306.
    [Test]
    public async Task StateWithNeitherBaseNorInterface_ReportsError()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            [StateRegistry.RegisterState("bad")]
            public partial class BadState : IHasIdentification { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0306", 1);
    }

    // Edge: a type with no registration attribute is never a shape-guard subject.
    [Test]
    public async Task UnregisteredType_NoDiagnostic()
    {
        var code = """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            public class PlainType : IHasIdentification { }
            """;
        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }
}
