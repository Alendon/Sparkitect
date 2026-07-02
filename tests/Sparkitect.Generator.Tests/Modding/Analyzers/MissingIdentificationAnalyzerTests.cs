using System.Threading.Tasks;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

/// <summary>
/// SPARK0263 fires on a registered concrete (carries a registration attribute implementing
/// <c>IRegisterMarker</c>) that is missing an explicit <c>: IHasIdentification</c> in user source.
/// </summary>
public sealed class MissingIdentificationAnalyzerTests : AnalyzerTestBase<MissingIdentificationAnalyzer>
{
    private const string RegisterAttr = """
        using Sparkitect.Modding;

        namespace DiTest;

        public sealed class RegisterThingAttribute : System.Attribute, IRegisterMarker { }
        """;

    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.ModdingCode);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(("RegisterAttr.cs", RegisterAttr));
    }

    [Test]
    public async Task RegisteredConcrete_WithExplicitIdentification_NoDiagnostic()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [RegisterThing]
        public sealed class Registered : IHasIdentification { }
        """;

        TestSources.Add(("Registered.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task RegisteredConcrete_MissingIdentification_ReportsSPARK0263()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [RegisterThing]
        public sealed class RegisteredMissing { }
        """;

        TestSources.Add(("RegisteredMissing.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0263", 1);
    }

    [Test]
    public async Task RegisteredConcrete_IdentifiedViaBaseClass_NoDiagnostic()
    {
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public abstract class IdentifiedBase : IHasIdentification { }

        [RegisterThing]
        public sealed class DerivedRegistered : IdentifiedBase { }
        """;

        TestSources.Add(("DerivedRegistered.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task NotRegistered_MissingIdentification_NoDiagnostic()
    {
        // No registration attribute — the analyzer only flags registered concretes.
        var code = """
        namespace DiTest;

        public sealed class PlainType { }
        """;

        TestSources.Add(("PlainType.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task AbstractRegistered_MissingIdentification_NoDiagnostic()
    {
        // Abstract types are not concrete registrations — skipped even when the attribute is present.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [RegisterThing]
        public abstract class AbstractRegistered { }
        """;

        TestSources.Add(("AbstractRegistered.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }
}
