using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.GameState;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.GameState;

public class StateServiceFactoryGeneratorTests : SourceGeneratorTestBase<StateServiceFactoryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.GameStateAttributes);
    }

    public override ModBuildSettings BuildSettings { get; }

    [Test]
    public async Task StateServiceFactoryGenerator_SimpleService_GeneratesFactory(CancellationToken token)
    {
        TestSources.Add(("TestStateService.cs",
            """
            using Sparkitect.GameState;

            namespace StateServiceTest;

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            [StateService<ITestFacade>]
            public class TestStateService : ITestFacade
            {
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task StateServiceFactoryGenerator_ServiceWithDependencies_GeneratesFactory(CancellationToken token)
    {
        TestSources.Add(("TestStateService.cs",
            """
            using Sparkitect.GameState;

            namespace StateServiceTest;

            public interface IOtherService { }

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            [StateService<ITestFacade>]
            public class TestStateService : ITestFacade
            {
                public TestStateService(IOtherService other)
                {
                }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractServiceFactoryModelData_SimpleService_ExtractsCorrectly(CancellationToken token)
    {
        TestSources.Add(("TestStateService.cs",
            """
            using Sparkitect.GameState;

            namespace StateServiceTest;

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            [StateService<ITestFacade>]
            public class TestStateService : ITestFacade
            {
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("StateServiceTest.TestStateService");

        await Assert.That(type).IsNotNull();

        var model = StateServiceFactoryGenerator.ExtractServiceFactoryModelData(type!);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ServiceType).IsEqualTo("global::StateServiceTest.ITestFacade");
        await Assert.That(model.ImplementationTypeName).IsEqualTo("TestStateService");
        await Assert.That(model.ImplementationNamespace).IsEqualTo("StateServiceTest");
        await Assert.That(model.ConstructorArguments).IsEmpty();
        await Assert.That(model.RequiredProperties).IsEmpty();
    }
}
