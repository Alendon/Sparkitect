using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.Stateless;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.Stateless;

public class StatelessFunctionGeneratorTests : SourceGeneratorTestBase<StatelessFunctionGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.ModdingCode);
        TestSources.Add(TestData.StatelessCoreTypes);
        TestSources.Add(TestData.StatelessTestTypes);

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
    public async Task TryExtractStatelessFunction_ValidMethod_GeneratesWrapper(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("init")]
                [TestScheduling]
                public static void Initialize() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();

        var generatedFiles = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        await Assert.That(generatedFiles.Any(f => f.Contains("Wrapper"))).IsTrue();
        await Assert.That(generatedFiles.Any(f => f.Contains("Registration"))).IsTrue();
        await Assert.That(generatedFiles.Any(f => f.Contains("Scheduling"))).IsTrue();
    }

    [Test]
    public async Task TryExtractStatelessFunction_MethodWithDIParams_GeneratesWrapperWithParams(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public interface ITestService { }

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("process")]
                [TestScheduling]
                public static void Process(ITestService service) { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();

        var wrapperTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Wrapper"));

        await Assert.That(wrapperTree).IsNotNull();

        var wrapperCode = wrapperTree!.GetText().ToString();
        await Assert.That(wrapperCode).Contains("ITestService");
    }

    [Test]
    public async Task TryExtractStatelessFunction_MethodWithOrderingAttrs_GeneratesSchedulingWithOrdering(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("first")]
                [TestScheduling]
                public static void First() { }

                [TestFunction("second")]
                [TestScheduling]
                [OrderAfter<TestModule.FirstFunc>]
                public static void Second() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        await Assert.That(driverRunResult.GeneratedTrees).IsNotEmpty();

        var schedulingTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Scheduling"));

        await Assert.That(schedulingTree).IsNotNull();

        var schedulingCode = schedulingTree!.GetText().ToString();
        await Assert.That(schedulingCode).Contains("OrderAfterAttribute");
        await Assert.That(schedulingCode).Contains("FirstFunc");
    }

    [Test]
    public async Task StatelessFunctionGenerator_SingleFunction_GeneratesWrapperAndRegistration(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("init")]
                [TestScheduling]
                public static void Initialize() { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task StatelessFunctionGenerator_MultipleFunctionsInClass_GeneratesAll(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("init")]
                [TestScheduling]
                public static void Initialize() { }

                [TestFunction("update")]
                [TestScheduling]
                [OrderAfter<TestModule.InitFunc>]
                public static void Update() { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task StatelessFunctionGenerator_FunctionWithDIParams_GeneratesCorrectWrapper(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public interface ILogger { void Log(string message); }
            public interface IConfig { string GetValue(string key); }

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("process")]
                [TestScheduling]
                public static void Process(ILogger logger, IConfig config) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }
}
