using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.ECS;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.ECS;

public class EcsQueryGeneratorTests : SourceGeneratorTestBase<EcsQueryGenerator>
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

    // --- Pipeline 1: Query Class Generation Tests (Phase 42) ---

    [Test]
    public async Task SingleReadComponents_GeneratesQueryClass(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            partial class SimpleQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedFiles = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        await Assert.That(generatedFiles.Any(f => f.Contains("SimpleQuery.g.cs"))).IsTrue();

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task MultipleStackedReadComponents_MergesInOrder(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position, Velocity>]
            [ReadComponents<Health>]
            partial class MultiReadQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiReadQuery.g.cs"));

        await Assert.That(generatedTree).IsNotNull();

        var code = generatedTree!.GetText().ToString();
        await Assert.That(code).Contains("Position.Identification");
        await Assert.That(code).Contains("Velocity.Identification");
        await Assert.That(code).Contains("Health.Identification");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ReadAndWriteComponents_GeneratesCorrectAccessors(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            [WriteComponents<Velocity>]
            partial class ReadWriteQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ReadWriteQuery.g.cs"));

        await Assert.That(generatedTree).IsNotNull();

        var code = generatedTree!.GetText().ToString();
        await Assert.That(code).Contains("ref readonly");
        await Assert.That(code).Contains("GetPosition()");
        await Assert.That(code).Contains("GetVelocity()");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ReadWriteExclude_GeneratesAllMetadata(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            [WriteComponents<Velocity>]
            [ExcludeComponents<EnemyTag>]
            partial class FullQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("FullQuery.g.cs"));

        await Assert.That(generatedTree).IsNotNull();

        var code = generatedTree!.GetText().ToString();
        await Assert.That(code).Contains("ReadComponentIds");
        await Assert.That(code).Contains("WriteComponentIds");
        await Assert.That(code).Contains("ExcludeComponentIds");
        await Assert.That(code).Contains("EnemyTag.Identification");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExposeKeyRequired_GeneratesKeyedQuery(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position, Velocity>]
            [ExposeKey<EntityId>(true)]
            partial class KeyedQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("KeyedQuery.g.cs"));

        await Assert.That(generatedTree).IsNotNull();

        var code = generatedTree!.GetText().ToString();
        await Assert.That(code).Contains("ComponentSetRequirement<");
        await Assert.That(code).Contains("Key");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task NoComponentQueryAttribute_NoOutput(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ReadComponents<Position>]
            partial class NotAQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var queryFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("NotAQuery"))
            .ToList();

        await Assert.That(queryFiles).IsEmpty();
    }

    [Test]
    public async Task CompilerGeneratedAttribute_SkipsTarget(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;
            using System.Runtime.CompilerServices;

            namespace TestMod;

            [CompilerGenerated]
            [ComponentQuery]
            [ReadComponents<Position>]
            partial class GeneratedQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var queryFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("GeneratedQuery"))
            .ToList();

        await Assert.That(queryFiles).IsEmpty();
    }

    [Test]
    public async Task InvalidCombination_SameComponentInReadAndWrite_SilentDrop(CancellationToken token)
    {
        TestSources.Add(("TestQuery.cs",
            """
            using Sparkitect.ECS.Queries;
            using Sparkitect.Modding;

            namespace TestMod;

            [ComponentQuery]
            [ReadComponents<Position>]
            [WriteComponents<Position>]
            partial class InvalidQuery;
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var queryFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("InvalidQuery"))
            .ToList();

        await Assert.That(queryFiles).IsEmpty();
    }

    // --- Pipeline 2: Resolution Metadata Generation Tests (Phase 43) ---

    [Test]
    public async Task SingleQueryParameter_GeneratesMetadataEntrypoint(CancellationToken token)
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

        var code = metadataFiles[0].GetText().ToString();
        await Assert.That(code).Contains("SgQueryMetadata");
        await Assert.That(code).Contains("SimpleQuery");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task MultipleQueryParameters_GeneratesMetadataEntrypoint(CancellationToken token)
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

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task QueryWithFrameTimingAndCommandBuffer_ExcludesNonQueryParams(CancellationToken token)
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
        await Assert.That(code).Contains("SimpleQuery");
        await Assert.That(code).DoesNotContain("FrameTimingHolder");
        await Assert.That(code).DoesNotContain("ICommandBufferAccessor");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task NonEcsMethod_NoMetadataOutput(CancellationToken token)
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
    public async Task MethodWithNoQueryParameters_NoMetadataOutput(CancellationToken token)
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
}
