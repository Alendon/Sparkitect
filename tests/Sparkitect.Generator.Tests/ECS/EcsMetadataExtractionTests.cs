using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Generator.ECS;

namespace Sparkitect.Generator.Tests.ECS;

public class EcsMetadataExtractionTests : SourceGeneratorTestBase<EcsQueryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.MetadataTypes);
        TestSources.Add(TestData.StatelessCoreTypes);
        TestSources.Add(TestData.ECS.EcsTypes);
        TestSources.Add(TestData.ECS.EcsAttributes);
        TestSources.Add(TestData.ECS.SampleComponents);
        TestSources.Add(TestData.ECS.EcsSystemStubs);

        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig", """
            is_global = true
            build_property.ModName = Test Mod
            build_property.ModId = test_mod
            build_property.RootNamespace = TestMod
            build_property.SgOutputNamespace = TestMod.Generated
            """));
    }

    [Test]
    public async Task SingleQueryParameter_GeneratesMetadataOutput(CancellationToken token)
    {
        TestSources.Add(("TestSystem.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.ECS.Systems;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            partial class SimpleQuery;

            public class TestGroup : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 100);

                [EcsSystemFunction("movement")]
                [EcsSystemScheduling]
                public static void MovementSystem(SimpleQuery query) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("ResolutionMetadata"))
            .ToList();

        await Assert.That(metadataFiles.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task MultipleQueryParameters_GeneratesMetadataOutput(CancellationToken token)
    {
        TestSources.Add(("TestSystem.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.ECS.Systems;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            partial class QueryA;

            [ComponentQuery]
            [ReadComponents<Velocity>]
            partial class QueryB;

            [ComponentQuery]
            [ReadComponents<Health>]
            partial class QueryC;

            public class TestGroup : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 100);

                [EcsSystemFunction("render_data")]
                [EcsSystemScheduling]
                public static void RenderDataSystem(QueryA a, QueryB b, QueryC c) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("ResolutionMetadata"))
            .ToList();

        await Assert.That(metadataFiles.Count).IsGreaterThanOrEqualTo(1);

        var code = metadataFiles[0].GetText().ToString();
        await Assert.That(code).Contains("QueryA");
        await Assert.That(code).Contains("QueryB");
        await Assert.That(code).Contains("QueryC");
    }

    [Test]
    public async Task FrameTimingAndCommandBuffer_ExcludedFromMetadata(CancellationToken token)
    {
        TestSources.Add(("TestSystem.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.ECS.Systems;
            using Sparkitect.ECS.Commands;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            partial class SimpleQuery;

            public class TestGroup : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 100);

                [EcsSystemFunction("movement")]
                [EcsSystemScheduling]
                public static void MovementSystem(SimpleQuery query, FrameTimingHolder ft, ICommandBufferAccessor cb) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("ResolutionMetadata"))
            .ToList();

        await Assert.That(metadataFiles.Count).IsGreaterThanOrEqualTo(1);

        var code = metadataFiles[0].GetText().ToString();
        await Assert.That(code).DoesNotContain("FrameTimingHolder");
        await Assert.That(code).DoesNotContain("ICommandBufferAccessor");
    }

    [Test]
    public async Task MethodWithoutSfAttribute_NoMetadataOutput(CancellationToken token)
    {
        TestSources.Add(("TestSystem.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            partial class SimpleQuery;

            public class TestGroup : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 100);

                [Obsolete]
                public static void NotASystem(SimpleQuery query) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("ResolutionMetadata"))
            .ToList();

        await Assert.That(metadataFiles).IsEmpty();
    }

    [Test]
    public async Task MethodWithSfAttributeButNoQueryParams_NoMetadataOutput(CancellationToken token)
    {
        TestSources.Add(("TestSystem.cs",
            """
            using Sparkitect.ECS.Systems;
            using Sparkitect.ECS.Commands;
            using Sparkitect.Modding;

            namespace TestMod;

            public class TestGroup : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 100);

                [EcsSystemFunction("timing_only")]
                [EcsSystemScheduling]
                public static void TimingOnlySystem(FrameTimingHolder ft, ICommandBufferAccessor cb) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("ResolutionMetadata"))
            .ToList();

        await Assert.That(metadataFiles).IsEmpty();
    }

    [Test]
    public async Task EcsQueryMetadataModel_RenderCodeLines_ProducesCorrectOutput(CancellationToken token)
    {
        var model = new EcsQueryMetadataModel("global::TestMod.SimpleQuery");
        var lines = model.RenderCodeLines();

        await Assert.That(lines.Count).IsEqualTo(6);
        await Assert.That(lines[0]).Contains("dependencies.TryAdd(typeof(global::TestMod.SimpleQuery)");
        await Assert.That(lines[2]).Contains("SgQueryMetadata<global::TestMod.SimpleQuery>");
        await Assert.That(lines[3]).Contains("global::TestMod.SimpleQuery.ReadComponentIds");
        await Assert.That(lines[4]).Contains("global::TestMod.SimpleQuery.WriteComponentIds");
        await Assert.That(lines[5]).Contains("world => new global::TestMod.SimpleQuery(world)");
    }
}
