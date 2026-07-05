using System.Threading.Tasks;
using Sparkitect.Generator.Metadata.Analyzers;

namespace Sparkitect.Generator.Tests.Metadata;

/// <summary>
/// Golden suite pinning MetadataParameterAnalyzer (SPARK0701): orphan ordering/parent triggers,
/// co-located-category absents (SC3 guard), the SF ParentId negative, the exemption hatch, the
/// zero-code new-category proof, and single-reporting of the orphan shape.
/// </summary>
public class MetadataParameterAnalyzerTests : AnalyzerTestBase<MetadataParameterAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.MetadataTypes);
        TestSources.Add(MetadataParameterTestData.Markers);
        TestSources.Add(MetadataParameterTestData.SchedulingAndOrdering);
        TestSources.Add(MetadataParameterTestData.SystemGroupCategory);
        TestSources.Add(MetadataParameterTestData.NavStepCategory);
    }

    // TRIGGER cases — orphan metadata parameter attributes report SPARK0701.

    [Test]
    public async Task OrderBeforeOnMethodWithoutScheduling_ReportsSPARK0701()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            public class Owner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [StatelessTestFunction]
                [OrderBefore<NamedTarget>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }

    [Test]
    public async Task OrderAfterOnMethodWithoutScheduling_ReportsSPARK0701()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            public class Owner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [StatelessTestFunction]
                [OrderAfter<NamedTarget>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }

    [Test]
    public async Task OrderBeforeOnClassWithoutSystemGroup_ReportsSPARK0701()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            [OrderBefore<NamedTarget>]
            public class GroupLike
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }

    [Test]
    public async Task ParentIdOnClassWithoutHarvestingCategory_ReportsSPARK0701()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            [ParentId<NamedTarget>]
            public class GroupLike
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }

    // ABSENT cases — a co-located harvesting category silences SPARK0701 (SC3 guard).

    [Test]
    public async Task OrderBeforeOnMethodWithScheduling_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            public class Owner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [StatelessTestFunction]
                [TestScheduling]
                [OrderBefore<NamedTarget>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    [Test]
    public async Task OrderingAndParentOnClassWithSystemGroup_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using Sparkitect.ECS.Systems;
            using MetadataParamTest;

            [SystemGroupScheduling]
            [OrderBefore<NamedTarget>]
            [OrderAfter<NamedTarget>]
            [ParentId<NamedTarget>]
            public class GameplayGroup
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    [Test]
    public async Task ParentIdOnStatelessFunctionMethodWithScheduling_NoDiagnostic()
    {
        // [ParentId<>] on a stateless-function method is SF-owned; no method-scope category
        // harvests ParentId, so it is never method-harvestable and must never be flagged here.
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            public class Owner
            {
                [StatelessTestFunction]
                [TestScheduling]
                [ParentId<NamedTarget>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    [Test]
    public async Task OrderBeforeOnExemptClass_NoDiagnostic()
    {
        // The opt-out attribute suppresses placement analysis on an otherwise-orphan type.
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Metadata;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            [AllowUnharvestedMetadataParameters]
            [OrderBefore<NamedTarget>]
            public class GroupLike
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    [Test]
    public async Task SymbolWithoutParameterAttributes_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            public class Owner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [StatelessTestFunction]
                [TestScheduling]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    // A brand-new category is validated with zero analyzer changes.

    [Test]
    public async Task OrderBeforeOnClassWithNewCategory_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Stateless;
            using MetadataParamTest;
            using NavTest;

            [NavStep]
            [OrderBefore<NamedTarget>]
            public class NavNode
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 0);
    }

    [Test]
    public async Task OrderBeforeOnClassWithoutNewCategory_ReportsSPARK0701()
    {
        var code = """
            using Sparkitect.Stateless;
            using MetadataParamTest;

            [OrderBefore<NamedTarget>]
            public class NavNode
            {
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }

    // No double report — the exact orphan-ordering-on-method shape that the stateless analyzer
    // now leaves alone (SPARK0405 == 0) is reported here exactly once (SPARK0701 == 1).

    [Test]
    public async Task OrphanOrderingOnMethod_ReportedExactlyOnce()
    {
        var code = """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using MetadataParamTest;

            public class Owner : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [StatelessTestFunction]
                [OrderBefore<NamedTarget>]
                public static void MyMethod() { }
            }
            """;

        TestSources.Add(("Test.cs", code));

        var diagnostics = await RunAnalyzerAsync();

        await AssertDiagnosticCount(diagnostics, "SPARK0701", 1);
    }
}
