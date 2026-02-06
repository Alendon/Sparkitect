using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.GameState;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.GameState;

public class StateModuleServiceGeneratorTests : SourceGeneratorTestBase<StateModuleServiceGenerator>
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
    public async Task StateModuleServiceGenerator_SingleServiceSingleModule_GeneratesConfigurator(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace StateServiceTest;

            public sealed partial class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static Span<Identification> RequiredModules => [];
            }

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            [StateService<ITestFacade, TestModule>]
            public class TestStateService : ITestFacade
            {
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task StateModuleServiceGenerator_MultipleServicesOneModule_GeneratesConfigurator(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace StateServiceTest;

            public sealed partial class CoreModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static Span<Identification> RequiredModules => [];
            }

            public interface IServiceA { }
            public interface IServiceB { }
            public interface IServiceC { }

            [StateService<IServiceA, CoreModule>]
            public class ServiceA : IServiceA { }

            [StateService<IServiceB, CoreModule>]
            public class ServiceB : IServiceB { }

            [StateService<IServiceC, CoreModule>]
            public class ServiceC : IServiceC { }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task StateModuleServiceGenerator_MultipleModules_GeneratesMultipleConfigurators(CancellationToken token)
    {
        TestSources.Add(("TestModules.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace StateServiceTest;

            public sealed partial class CoreModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static Span<Identification> RequiredModules => [];
            }

            public sealed partial class RenderingModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static Span<Identification> RequiredModules => [];
            }

            public interface ICoreService { }
            public interface IRenderService { }

            [StateService<ICoreService, CoreModule>]
            public class CoreService : ICoreService { }

            [StateService<IRenderService, RenderingModule>]
            public class RenderService : IRenderService { }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();
        await Verifier.Verify(driverRunResult, verifySettings);
    }
    
}
