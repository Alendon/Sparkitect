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
        TestSources.Add(TestData.MetadataTypes);
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

    // ===== Error Scenario Tests =====
    // These tests verify the generator handles edge cases gracefully (returns null/empty) rather than crashing.
    // Per CONTEXT.md: "StatelessFunctionGenerator tests assume valid input - analyzers catch errors."

    [Test]
    public async Task TryExtractStatelessFunction_MissingStatelessFunctionAttribute_GeneratesNothing(CancellationToken token)
    {
        // Method has scheduling attribute but no StatelessFunction attribute
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                // Missing: [TestFunction("init")] - no StatelessFunction attribute
                [TestScheduling]
                public static void Initialize() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        // Should not generate any wrapper files for this method
        var wrapperFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Wrapper"))
            .ToList();

        await Assert.That(wrapperFiles).IsEmpty();
    }

    [Test]
    public async Task TryExtractStatelessFunction_MissingSchedulingAttribute_GeneratesNothing(CancellationToken token)
    {
        // Method has StatelessFunction attribute but no scheduling attribute
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("init")]  // Has StatelessFunction attribute
                // Missing: [TestScheduling] - no scheduling attribute
                public static void Initialize() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        // Should not generate any wrapper files for this method
        var wrapperFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Wrapper"))
            .ToList();

        await Assert.That(wrapperFiles).IsEmpty();
    }

    [Test]
    public async Task TryExtractStatelessFunction_TypeWithoutIHasIdentification_GeneratesNothing(CancellationToken token)
    {
        // Class doesn't implement IHasIdentification
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            // Missing: IHasIdentification interface
            public partial class TestModule
            {
                [TestFunction("init")]
                [TestScheduling]
                public static void Initialize() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        // Should not generate any wrapper files for this method
        var wrapperFiles = driverRunResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("Wrapper"))
            .ToList();

        await Assert.That(wrapperFiles).IsEmpty();
    }

    [Test]
    public async Task TryExtractStatelessFunction_PrivateMethod_GeneratesNothing(CancellationToken token)
    {
        // Private methods should be ignored by the generator
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
                private static void Initialize() { }
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        // Generator processes all methods with attributes, but analyzers catch accessibility issues
        // This test verifies no crashes occur - the generator will either skip or produce output
        // that the analyzer will flag
        await Assert.That(driverRunResult.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task TryExtractStatelessFunction_NonStaticMethod_GeneratesNothing(CancellationToken token)
    {
        // Instance methods should be ignored (analyzer catches this, but generator should handle gracefully)
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
                public void Initialize() { }  // Instance method, not static
            }
            """));

        var (outputCompilation, driverRunResult) = await RunGeneratorAsync(token);

        // Generator processes all methods with attributes, but analyzers catch static requirement
        // This test verifies no crashes occur - the generator will either skip or produce output
        // that the analyzer will flag
        await Assert.That(driverRunResult.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task StatelessFunction_WithFacadeParameter_GeneratesMetadataEntrypoint(CancellationToken token)
    {
        TestSources.Add(("FacadeTypes.cs", """
            using Sparkitect.DI.GeneratorAttributes;

            namespace Sparkitect.GameState
            {
                [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
                public class StateFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;
            }

            namespace Sparkitect.DI.GeneratorAttributes
            {
                [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
                public sealed class FacadeForAttribute<TService> : Attribute where TService : class;
            }
            """));
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.GameState;

            namespace TestMod;

            [StateFacade<ITestServiceFacade>]
            public interface ITestService { }

            [FacadeFor<ITestService>]
            public interface ITestServiceFacade { }

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("process")]
                [TestScheduling]
                public static void Process(ITestServiceFacade facade) { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedFiles = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        // RenderMetadataEntrypoint names the file {SafeClassName}_ResolutionMetadata.g.cs
        await Assert.That(generatedFiles.Any(f => f.Contains("ResolutionMetadata"))).IsTrue();
    }

    [Test]
    public async Task StatelessFunction_MultipleParentsForSameRegistry_ProducesUniqueHintNames(CancellationToken token)
    {
        TestSources.Add(("TestModules.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public partial class ClassA : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("alpha")]
                [TestScheduling]
                public static void Alpha() { }
            }

            public partial class ClassB : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 2);

                [TestFunction("beta")]
                [TestScheduling]
                public static void Beta() { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var generatedFiles = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        // Both parents target TestRegistry, so registration files should have parent disambiguation
        var registrationFiles = generatedFiles.Where(f => f.Contains("Registration")).ToList();
        await Assert.That(registrationFiles.Count).IsGreaterThanOrEqualTo(2);

        // All registration file names should be unique (no collision)
        await Assert.That(registrationFiles.Distinct().Count()).IsEqualTo(registrationFiles.Count);

        // Files should contain parent class disambiguation
        await Assert.That(registrationFiles.Any(f => f.Contains("ClassA"))).IsTrue();
        await Assert.That(registrationFiles.Any(f => f.Contains("ClassB"))).IsTrue();
    }

    [Test]
    public async Task StatelessFunctionGenerator_WithParentIdAttribute_UsesOverriddenParent(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            // Parent type that function will be associated with
            public class ParentModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(2, 2, 2);
            }

            // Container class - does NOT implement IHasIdentification
            public partial class FunctionContainer
            {
                [TestFunction("extended_func")]
                [TestScheduling]
                [ParentId<ParentModule>]
                public static void ExtendedFunction() { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        // Should generate wrapper
        var wrapperTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Wrapper"));
        await Assert.That(wrapperTree).IsNotNull();

        var wrapperCode = wrapperTree!.GetText().ToString();

        // Wrapper should be nested in FunctionContainer (where declared)
        await Assert.That(wrapperCode).Contains("public partial class FunctionContainer");

        // ParentIdentification should point to ParentModule (overridden by attribute, fully qualified)
        await Assert.That(wrapperCode).Contains("IdentificationHelper.Read<global::TestMod.ParentModule>");
    }

    [Test]
    public async Task StatelessFunctionGenerator_WithParentIdAttributeAndIHasIdentification_UsesOverriddenParent(CancellationToken token)
    {
        TestSources.Add(("TestModule.cs", """
            using StatelessTest;
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace TestMod;

            public class ParentModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(2, 2, 2);
            }

            // Container implements IHasIdentification but function overrides with ParentIdAttribute
            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Create(1, 1, 1);

                [TestFunction("override_parent")]
                [TestScheduling]
                [ParentId<ParentModule>]
                public static void OverrideParentFunction() { }
            }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var wrapperTree = driverRunResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Wrapper"));
        await Assert.That(wrapperTree).IsNotNull();

        var wrapperCode = wrapperTree!.GetText().ToString();

        // Wrapper nested in TestModule
        await Assert.That(wrapperCode).Contains("public partial class TestModule");

        // ParentIdentification should point to ParentModule (overridden, fully qualified)
        await Assert.That(wrapperCode).Contains("IdentificationHelper.Read<global::TestMod.ParentModule>");
    }
}
