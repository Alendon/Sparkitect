using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.DI;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.DI;

public class SingletonContainerGeneratorTests : SourceGeneratorTestBase<SingletonContainerGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);
        
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.DiAttributes);
        
        // Add analyzer config for ModBuildSettings
        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig", 
            """
            is_global = true
            build_property.ModName = TestMod
            build_property.RootNamespace = DiTest
            """));
    }
    
    [Test]
    public async Task SingletonGenerator_FullRun_NoDependencies(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface ITestService {}

            [Singleton<ITestService>]
            public class TestService : ITestService {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractSingletonModel_BasicSingleton(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface ITestService {}

            [Singleton<ITestService>]
            public class TestService : ITestService {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.TestService");
        await Assert.That(type).IsNotNull();

        var symbol = compilation.GetTypeByMetadataName("DiTest.TestService");
        await Assert.That(symbol).IsNotNull();

        var model = SingletonContainerGenerator.ExtractSingletonModel(symbol!);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.FactoryFullName).IsEqualTo("global::DiTest.TestService_Factory");
    }

    [Test]
    public async Task CreateContainerModel_SingleSingleton(CancellationToken token)
    {
        var singletons = ImmutableArray.Create(
            new SingletonModel("global::DiTest.TestService_Factory")
        );

        var buildSettings = new ModBuildSettings("TestMod", "DiTest", false, "", "");

        var containerModel = SingletonContainerGenerator.CreateContainerModel(singletons, buildSettings);

        await Assert.That(containerModel.ConfiguratorClassName).IsEqualTo("DiTestConfigurator");
        await Assert.That(containerModel.Namespace).IsEqualTo("DiTest");
        var item = await Assert.That(containerModel.Singletons).HasSingleItem();
        await Assert.That(item).IsNotNull();
        await Assert.That(item!.FactoryFullName).IsEqualTo("global::DiTest.TestService_Factory");
    }

    [Test]
    public async Task CreateContainerModel_EmptyRootNamespace_UsesDefault(CancellationToken token)
    {
        var singletons = ImmutableArray.Create(
            new SingletonModel("global::DiTest.TestService_Factory")
        );

        var buildSettings = new ModBuildSettings("TestMod", "", false, "", "");

        var containerModel = SingletonContainerGenerator.CreateContainerModel(singletons, buildSettings);

        await Assert.That(containerModel.ConfiguratorClassName).IsEqualTo("GeneratedConfigurator");
        await Assert.That(containerModel.Namespace).IsEqualTo("Generated");
    }

    [Test]
    public async Task RenderCoreConfigurator_SingleSingleton(CancellationToken token)
    {
        var containerModel = new SingletonContainerModel(
            "DiTestConfigurator",
            "DiTest",
            [new SingletonModel("global::DiTest.TestService_Factory")]
        );

        var success = SingletonContainerGenerator.RenderCoreConfigurator(containerModel, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("DiTestConfigurator.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderCoreConfigurator_MultipleSingletons(CancellationToken token)
    {
        var containerModel = new SingletonContainerModel(
            "DiTestConfigurator",
            "DiTest",
            [
                new SingletonModel("global::DiTest.TestService_Factory"),
                new SingletonModel("global::DiTest.AnotherService_Factory")
            ]
        );

        var success = SingletonContainerGenerator.RenderCoreConfigurator(containerModel, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("DiTestConfigurator.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task SingletonGenerator_FullRun_MultipleSingletons(CancellationToken token)
    {
        TestSources.Add(("Services.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface ITestService {}
            public interface IAnotherService {}

            [Singleton<ITestService>]
            public class TestService : ITestService {}

            [Singleton<IAnotherService>]
            public class AnotherService : IAnotherService {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }
}
