using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.GameState;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.GameState;

public class StateMethodGeneratorTests : SourceGeneratorTestBase<StateMethodGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.GameStateAttributes);
    }

    public override ModBuildSettings BuildSettings { get; }

    [Test]
    public async Task StateMethodGenerator_FullRun_HappyPath(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public interface ITestService { }

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            public sealed partial class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [typeof(ITestService), typeof(ITestFacade)];

                [StateFunction("init")]
                [OnModuleEnter]
                public static void Initialize(ITestService service)
                {
                }

                [StateFunction("update")]
                [PerFrame]
                [OrderAfter("init")]
                public static void Update(ITestService service, ITestFacade facade)
                {
                }

                [StateFunction("cleanup")]
                [OnModuleExit]
                [OrderBefore("update")]
                public static void Cleanup()
                {
                }
            }

            public sealed partial class AnotherModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static IReadOnlyList<Type> UsedServices => [];

                [StateFunction("process")]
                [OnStateEnter]
                [OrderAfter<TestModule>("init")]
                public static void Process()
                {
                }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractStateModuleModel_ValidModule(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public interface ITestService { }

            public sealed class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [typeof(ITestService)];

                [StateFunction("test_func")]
                [PerFrame]
                public static void TestFunction(ITestService service)
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.TestModule");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateModuleModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ModuleTypeName).IsEqualTo("TestModule");
        await Assert.That(model.ModuleNamespace).IsEqualTo("GameStateTest");
        await Assert.That(model.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.FunctionKey).IsEqualTo("test_func");
        await Assert.That(function.Schedule).IsEqualTo(StateMethodSchedule.PerFrame);
        await Assert.That(function.Parameters).HasCount().EqualTo(1);
        await Assert.That(function.Parameters[0].ParameterType).IsEqualTo("global::GameStateTest.ITestService");
    }

    [Test]
    public async Task ExtractStateModuleModel_WithFacadeParameter(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            [StateFacade<ITestFacade>]
            public interface ITestFacade { }

            public sealed class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Type> UsedServices => [typeof(ITestFacade)];

                [StateFunction("test_func")]
                [OnStateEnter]
                public static void TestFunction(ITestFacade facade)
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.TestModule");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateModuleModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.Parameters).HasCount().EqualTo(1);
    }
}