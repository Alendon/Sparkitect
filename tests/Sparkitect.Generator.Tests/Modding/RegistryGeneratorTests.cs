using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.Modding;
using VerifyTUnit;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);

        // Add analyzer config for ModBuildSettings
        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = TestMod
            build_property.RootNamespace = DiTest
            build_property.SgOutputNamespace = DiTest.Generated
            """));
    }

    [Test]
    public async Task RegistryGenerator_FullRun_SingleRegistry(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [Registry(Identifier = "test")]
            public class TestRegistry : IRegistry {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task RegistryGenerator_FullRun_MultipleRegistries(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [Registry(Identifier = "test1")]
            public class TestRegistry1 : IRegistry {}

            [Registry(Identifier = "test2")]
            public class TestRegistry2 : IRegistry {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractModel_Valid(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            namespace DiTest;


            [Registry(Identifier = "test")]
            public class TestRegistry : IRegistry {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var registryType = compilation.GetTypeByMetadataName("DiTest.TestRegistry");
        var att = registryType?.GetAttributes().First();


        var result = RegistryGenerator.ExtractModel(registryType!, att!);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TypeName).IsEqualTo("TestRegistry");
        await Assert.That(result.Key).IsEqualTo("test");
        await Assert.That(result.ContainingNamespace).IsEqualTo("DiTest");
    }

    [Test]
    public async Task ExtractFromMetadata_Valid(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [assembly: RegistryMetadataAttribute<TestMetadata>]

            public class TestMetadata {
                public const string TypeName = "TestRegistry";
                public const string Key = "test";
                public const string ContainingNamespace = "DiTest";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var registryType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");


        var success =
            RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(registryType!, compilation, out var model);
        await Assert.That(success).IsTrue();

        await Assert.That(model!.TypeName).IsEqualTo("TestRegistry");
        await Assert.That(model.Key).IsEqualTo("test");
        await Assert.That(model.ContainingNamespace).IsEqualTo("DiTest");
    }

    [Test]
    public async Task ExtractModel_NullNamespace_ReturnsNull(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            // No namespace - class at global level

            [Registry(Identifier = "test")]
            public class TestRegistry : IRegistry {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var registryType = compilation.GetTypeByMetadataName("TestRegistry");
        var att = registryType?.GetAttributes().First();

        var result = RegistryGenerator.ExtractModel(registryType!, att!);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ExtractModel_NullKey_ReturnsNull(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [Registry()] // No Identifier provided
            public class TestRegistry : IRegistry {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var registryType = compilation.GetTypeByMetadataName("DiTest.TestRegistry");
        var att = registryType?.GetAttributes().First();

        var result = RegistryGenerator.ExtractModel(registryType!, att!);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryExtractRegistryFromAssemblyAttribute_MissingField_ReturnsFalse(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            public class TestMetadata {
                public const string TypeName = "TestRegistry";
                // Missing Key field
                public const string ContainingNamespace = "DiTest";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var metadataType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");

        var success = RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(metadataType!, compilation, out var model);

        await Assert.That(success).IsFalse();
        await Assert.That(model).IsNull();
    }

    [Test]
    public async Task TryExtractRegistryFromAssemblyAttribute_NonConstField_ReturnsFalse(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            public class TestMetadata {
                public const string TypeName = "TestRegistry";
                public static string Key = "test"; // Not const
                public const string ContainingNamespace = "DiTest";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var metadataType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");

        var success = RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(metadataType!, compilation, out var model);

        await Assert.That(success).IsFalse();
        await Assert.That(model).IsNull();
    }
}