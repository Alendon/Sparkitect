using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.Metadata;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.Metadata;

public class MetadataGeneratorTests : SourceGeneratorTestBase<MetadataGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.MetadataTypes);
        TestSources.Add(MetadataTestData.ApplyMetadataEntrypointTypes);
        TestSources.Add(MetadataTestData.TestMetadataTypes);

        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig", """
            is_global = true
            build_property.ModName = Test Mod
            build_property.ModId = test_mod
            build_property.RootNamespace = TestMod
            build_property.SgOutputNamespace = TestMod.Generated
            """));
    }

    public override ModBuildSettings BuildSettings => new("Test Mod", "test_mod",
        "TestMod", false, "TestMod.Generated");

    [Test]
    public async Task MetadataGenerator_TypeWithMetadataAttribute_GeneratesEntrypoint(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using MetadataTest;
            using Sparkitect.Modding;

            namespace TestMod;

            [TestMetadata]
            public partial class TestEntity : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedFiles = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        await Assert.That(generatedFiles.Any(f => f.Contains("TestEntity_Metadata"))).IsTrue();
    }

    [Test]
    public async Task MetadataGenerator_TypeWithMetadataAttribute_GeneratesCorrectContent(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using MetadataTest;
            using Sparkitect.Modding;

            namespace TestMod;

            [TestMetadata]
            public partial class TestEntity : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestEntity_Metadata"));

        await Assert.That(metadataTree).IsNotNull();

        var code = metadataTree!.GetText().ToString();
        await Assert.That(code).Contains("ApplyMetadataEntrypoint<global::MetadataTest.TestMetadataType>");
        await Assert.That(code).Contains("ApplyMetadataEntrypointAttribute<global::MetadataTest.TestMetadataType>");
        await Assert.That(code).Contains("CollectMetadata");
        await Assert.That(code).Contains("IdentificationHelper.Read<global::TestMod.TestEntity>");
        await Assert.That(code).Contains("new global::MetadataTest.TestMetadataType(");
    }

    [Test]
    public async Task MetadataGenerator_TypeWithoutIHasIdentification_GeneratesNothing(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using MetadataTest;

            namespace TestMod;

            [TestMetadata]
            public partial class NotIdentifiable
            {
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Metadata"))
            .ToList();

        await Assert.That(metadataFiles).IsEmpty();
    }

    [Test]
    public async Task MetadataGenerator_CompilerGeneratedType_IsExcluded(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using MetadataTest;
            using Sparkitect.Modding;
            using System.Runtime.CompilerServices;

            namespace TestMod;

            [CompilerGenerated]
            [TestMetadata]
            public partial class GeneratedEntity : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Metadata"))
            .ToList();

        await Assert.That(metadataFiles).IsEmpty();
    }

    [Test]
    public async Task MetadataGenerator_TypeWithNonMetadataAttribute_GeneratesNothing(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using Sparkitect.Modding;

            namespace TestMod;

            [System.Obsolete]
            public partial class TestEntity : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var metadataFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Metadata"))
            .ToList();

        await Assert.That(metadataFiles).IsEmpty();
    }

    [Test]
    public async Task MetadataGenerator_Snapshot_VerifiesGeneratedOutput(CancellationToken token)
    {
        TestSources.Add(("TestTarget.cs", """
            using MetadataTest;
            using Sparkitect.Modding;

            namespace TestMod;

            [TestMetadata]
            public partial class TestEntity : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }
}
