using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.DI;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.DI;

public class FacadeMappingGeneratorTests : SourceGeneratorTestBase<FacadeMappingGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.GameStateAttributes);
        TestSources.Add(TestData.ModdingCode);

        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig", """
            is_global = true
            build_property.ModName = Facade Test Mod
            build_property.ModId = facade_test
            build_property.RootNamespace = FacadeTest
            build_property.SgOutputNamespace = FacadeTest.Generated
            """));
    }

    public override ModBuildSettings BuildSettings => new("Facade Test Mod", "facade_test",
        "FacadeTest", false, "FacadeTest.Generated");

    [Test]
    public async Task FacadeMappingGenerator_SingleFacadeInterface_GeneratesConfigurator(CancellationToken token)
    {
        TestSources.Add(("TestFacade.cs",
            """
            using Sparkitect.GameState;

            namespace FacadeTest;

            [StateFacade<ITestFacade>]
            public interface ITestService
            {
            }

            public interface ITestFacade
            {
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task FacadeMappingGenerator_MultipleFacadesOnInterface_GeneratesAllMappings(CancellationToken token)
    {
        TestSources.Add(("TestFacade.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace FacadeTest;

            [StateFacade<IStateFacade>]
            [RegistryFacade<IRegistryFacade>]
            public interface ITestService
            {
            }

            public interface IStateFacade { }
            public interface IRegistryFacade { }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task FacadeMappingGenerator_MultipleInterfacesWithFacades_GeneratesUnifiedConfigurator(CancellationToken token)
    {
        TestSources.Add(("TestFacades.cs",
            """
            using Sparkitect.GameState;

            namespace FacadeTest;

            [StateFacade<IServiceAFacade>]
            public interface IServiceA { }

            [StateFacade<IServiceBFacade>]
            public interface IServiceB { }

            public interface IServiceAFacade { }
            public interface IServiceBFacade { }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task FacadeMappingGenerator_NoFacadeInterfaces_GeneratesNothing(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            namespace FacadeTest;

            public interface ITestService
            {
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractFacadeMappings_InterfaceWithStateFacade_ExtractsCorrectly(CancellationToken token)
    {
        TestSources.Add(("TestFacade.cs",
            """
            using Sparkitect.GameState;

            namespace FacadeTest;

            [StateFacade<ITestFacade>]
            public interface ITestService { }

            public interface ITestFacade { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var interfaceSymbol = compilation.GetTypeByMetadataName("FacadeTest.ITestService");

        await Assert.That(interfaceSymbol).IsNotNull();

        var mappings = FacadeMappingGenerator.ExtractFacadeMappings(interfaceSymbol!);

        await Assert.That(mappings).IsNotNull();
        await Assert.That(mappings!.ServiceInterfaceType).IsEqualTo("global::FacadeTest.ITestService");
        await Assert.That(mappings.FacadeMappings).HasCount().EqualTo(1);
        await Assert.That(mappings.FacadeMappings[0].FacadeType).IsEqualTo("global::FacadeTest.ITestFacade");
    }

    [Test]
    public async Task ExtractFacadeMappings_InterfaceWithRegistryFacade_ExtractsCorrectly(CancellationToken token)
    {
        TestSources.Add(("TestFacade.cs",
            """
            using Sparkitect.Modding;

            namespace FacadeTest;

            [RegistryFacade<ITestRegistryFacade>]
            public interface ITestService { }

            public interface ITestRegistryFacade { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var interfaceSymbol = compilation.GetTypeByMetadataName("FacadeTest.ITestService");

        await Assert.That(interfaceSymbol).IsNotNull();

        var mappings = FacadeMappingGenerator.ExtractFacadeMappings(interfaceSymbol!);

        await Assert.That(mappings).IsNotNull();
        await Assert.That(mappings!.FacadeMappings).HasCount().EqualTo(1);
        await Assert.That(mappings.FacadeMappings[0].FacadeType).IsEqualTo("global::FacadeTest.ITestRegistryFacade");
    }

    [Test]
    public async Task ExtractFacadeMappings_NonInterface_ReturnsNull(CancellationToken token)
    {
        TestSources.Add(("TestClass.cs",
            """
            namespace FacadeTest;

            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var classSymbol = compilation.GetTypeByMetadataName("FacadeTest.TestClass");

        await Assert.That(classSymbol).IsNotNull();

        var mappings = FacadeMappingGenerator.ExtractFacadeMappings(classSymbol!);

        await Assert.That(mappings).IsNull();
    }

    [Test]
    public async Task RenderFacadeConfigurator_WithSgOutputNamespace_UsesModNamespace()
    {
        var mappings = ImmutableArray.Create(
            new FacadeMapping("global::FacadeTest.ITestFacade", "global::FacadeTest.ITestService",
                "global::Sparkitect.GameState.StateFacadeAttribute"));

        var settings = new ModBuildSettings("MyMod", "my_mod", "MyMod", false, "MyMod.Generated");

        var result = FacadeMappingGenerator.RenderFacadeConfigurator(
            mappings, "global::Sparkitect.GameState.StateFacadeAttribute", settings, out var code, out _);

        await Assert.That(result).IsTrue();
        await Assert.That(code).Contains("namespace MyMod.Generated.CompilerGenerated.DI;");
    }

    [Test]
    public async Task RenderFacadeConfigurator_WithEmptySgOutputNamespace_FallsBackToMarkerNamespace()
    {
        var mappings = ImmutableArray.Create(
            new FacadeMapping("global::FacadeTest.ITestFacade", "global::FacadeTest.ITestService",
                "global::Sparkitect.GameState.StateFacadeAttribute"));

        var settings = new ModBuildSettings("MyMod", "my_mod", "MyMod", false, "");

        var result = FacadeMappingGenerator.RenderFacadeConfigurator(
            mappings, "global::Sparkitect.GameState.StateFacadeAttribute", settings, out var code, out _);

        await Assert.That(result).IsTrue();
        await Assert.That(code).Contains("namespace Sparkitect.GameState.CompilerGenerated.DI;");
    }
}
