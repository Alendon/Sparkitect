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
                public static Span<Identification> RequiredModules => [];

                [StateFunction("init")]
                [OnCreate]
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
                [OnDestroy]
                [OrderBefore("update")]
                public static void Cleanup()
                {
                }
            }

            public sealed partial class AnotherModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 2);
                public static Span<Identification> RequiredModules => [];

                [StateFunction("process")]
                [OnFrameEnter]
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
                public static Span<Identification> RequiredModules => [];

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

        var model = StateMethodGenerator.ExtractStateParentModel(type!, compilation, token);

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
                public static Span<Identification> RequiredModules => [];

                [StateFunction("test_func")]
                [OnFrameEnter]
                public static void TestFunction(ITestFacade facade)
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.TestModule");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateParentModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.Parameters).HasCount().EqualTo(1);
    }

    [Test]
    public async Task StateMethodGenerator_StateDescriptorWithFunction_GeneratesWrapper(CancellationToken token)
    {
        TestSources.Add(("TestDescriptor.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public interface ITestService { }

            public sealed partial class DesktopState : IStateDescriptor
            {
                public static Identification ParentId => Identification.Create(1, 1, 0);
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Identification> Modules => [];

                [StateFunction("desktop_init")]
                [OnFrameEnter]
                public static void Initialize(ITestService service)
                {
                }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractStateParentModel_StateDescriptor_ExtractsCorrectly(CancellationToken token)
    {
        TestSources.Add(("TestDescriptor.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public sealed class DesktopState : IStateDescriptor
            {
                public static Identification ParentId => Identification.Create(1, 1, 0);
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static IReadOnlyList<Identification> Modules => [];

                [StateFunction("desktop_init")]
                [OnFrameEnter]
                public static void Initialize()
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.DesktopState");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateParentModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ModuleTypeName).IsEqualTo("DesktopState");
        await Assert.That(model.ModuleNamespace).IsEqualTo("GameStateTest");
        await Assert.That(model.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.FunctionKey).IsEqualTo("desktop_init");
        await Assert.That(function.Schedule).IsEqualTo(StateMethodSchedule.OnFrameEnter);
    }

    [Test]
    public async Task ExtractStateModuleModel_LiteralKey_GeneratesConstField(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public sealed class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static Span<Identification> RequiredModules => [];

                [StateFunction("my_function")]
                [PerFrame]
                public static void MyFunction()
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.TestModule");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateParentModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.FunctionKey).IsEqualTo("my_function");
        await Assert.That(function.GenerateConstField).IsTrue();
        await Assert.That(function.KeyExpression).IsEqualTo("MyFunction_Key");
    }

    [Test]
    public async Task ExtractStateModuleModel_ConstKey_ReusesExistingConst(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs",
            """
            using Sparkitect.GameState;
            using Sparkitect.Modding;

            namespace GameStateTest;

            public sealed class TestModule : IStateModule
            {
                public static Identification Identification => Identification.Create(1, 1, 1);
                public static Span<Identification> RequiredModules => [];

                public const string InitKey = "init_value";

                [StateFunction(InitKey)]
                [OnCreate]
                public static void Initialize()
                {
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("GameStateTest.TestModule");

        await Assert.That(type).IsNotNull();

        var model = StateMethodGenerator.ExtractStateParentModel(type!, compilation, token);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.Functions).HasCount().EqualTo(1);

        var function = model.Functions[0];
        await Assert.That(function.FunctionKey).IsEqualTo("init_value");
        await Assert.That(function.GenerateConstField).IsFalse();
        await Assert.That(function.KeyExpression).Contains("InitKey");
    }
}