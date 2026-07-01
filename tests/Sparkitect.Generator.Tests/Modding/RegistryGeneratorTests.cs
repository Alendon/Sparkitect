using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Generator.Modding;
using VerifyTUnit;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);

        // Add analyzer config for ModBuildSettings
        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = Sample Test Mod
            build_property.ModId = sample_test
            build_property.RootNamespace = SampleTest
            build_property.SgOutputNamespace = SampleTest.Generated
            """));
    }

    public override ModBuildSettings BuildSettings => new("Sample Test Mod", "sample_test",
        "SampleTest", false, "SampleTest.Generated");


    [Test]
    public async Task ExtractModel_Valid(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            namespace DiTest;


            [Registry(Identifier = "test")]
            public class TestRegistry : IRegistry<TestModule> {}
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
                public const bool IsExternal = false;
                public const string RegisterMethods = "RegisterItem;RegisterType";
                public const string ResourceFiles = "data:1:0;config:0:0";
                
                public class RegisterItem {
                    public const string FunctionName = "RegisterItem";
                    public const int PrimaryParameterKind = 2; // Value
                    public const int Constraint = 0; // None
                    public const string TypeConstraint = "global::System.String";
                }
                
                public class RegisterType {
                    public const string FunctionName = "RegisterType";
                    public const int PrimaryParameterKind = 4; // Type
                    public const int Constraint = 1; // ReferenceType
                    public const string TypeConstraint = "global::System.IDisposable";
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var registryType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");


        var success =
            RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(registryType!, out var model);
        await Assert.That(success).IsTrue();

        await Assert.That(model!.TypeName).IsEqualTo("TestRegistry");
        await Assert.That(model.Key).IsEqualTo("test");
        await Assert.That(model.ContainingNamespace).IsEqualTo("DiTest");
        await Assert.That(model.RegisterMethods).HasCount().EqualTo(2);
        await Assert.That(model.ResourceFiles).HasCount().EqualTo(2);

        var registerItem = model.RegisterMethods.First(m => m.FunctionName == "RegisterItem");
        await Assert.That(registerItem.PrimaryParameterKind).IsEqualTo(PrimaryParameterKind.Value);
        await Assert.That(registerItem.Constraint).IsEqualTo(TypeConstraintFlag.None);
        await Assert.That(registerItem.TypeConstraint.First()).IsEqualTo("global::System.String");

        var resourceData = model.ResourceFiles.First(rf => rf.Key == "data");
        await Assert.That(resourceData.Required).IsTrue();
        var resourceConfig = model.ResourceFiles.First(rf => rf.Key == "config");
        await Assert.That(resourceConfig.Required).IsFalse();
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
            public class TestRegistry : IRegistry<TestModule> {}
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
            public class TestRegistry : IRegistry<TestModule> {}
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
                public const string RegisterMethods = "";
                public const string ResourceFiles = "";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var metadataType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");

        var success = RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(metadataType!, out var model);

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
                public const string RegisterMethods = "";
                public const string ResourceFiles = "";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var metadataType = compilation.GetTypeByMetadataName("DiTest.TestMetadata");

        var success = RegistryGenerator.TryExtractRegistryFromAssemblyAttribute(metadataType!, out var model);

        await Assert.That(success).IsFalse();
        await Assert.That(model).IsNull();
    }

    [Test]
    public async Task RenderRegistryMetadata_SimpleRegistry(CancellationToken token)
    {
        var model = new RegistryModel(
            "TestRegistry",
            "test",
            "DiTest",
            false,
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterItem", PrimaryParameterKind.Value, TypeConstraintFlag.None,
                    ImmutableValueArray.From("global::System.String")),
                new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type, TypeConstraintFlag.ReferenceType,
                    ImmutableValueArray.From("global::System.IDisposable"))
            ),
            ImmutableValueArray.From(
                ("data", true, false),
                ("config", false, false)
            )
        );

        var success = RegistryGenerator.RenderRegistryMetadata(model, BuildSettings, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestRegistry_Metadata.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderRegistryConfigurator_MultipleRegistries(CancellationToken token)
    {
        RegistryWithFactory[] registriesWithFactories =
        [
            new(
                new RegistryModel("TestRegistry1", "test1", "DiTest", false, [], []),
                new FactoryWithRegistration(
                    new FactoryModel("Sparkitect.Modding.IRegistryBase", "TestRegistry1", "DiTest", [], [],
                        new FactoryIntent.Keyed("test1"), []),
                    new RegistrationModel("global::DiTest.TestRegistry1_KeyedFactory", [], "\"test1\"")),
                []),
            new(
                new RegistryModel("TestRegistry2", "test2", "DiTest.Nested", false, [], []),
                new FactoryWithRegistration(
                    new FactoryModel("Sparkitect.Modding.IRegistryBase", "TestRegistry2", "DiTest.Nested", [], [],
                        new FactoryIntent.Keyed("test2"), []),
                    new RegistrationModel("global::DiTest.Nested.TestRegistry2_KeyedFactory", [], "\"test2\"")), [])
        ];

        var success =
            RegistryGenerator.RenderRegistryConfigurator([..registriesWithFactories], BuildSettings,
                out var configuratorCode, out var configuratorFileName,
                out var shellCode, out var shellFileName);

        await Assert.That(success).IsTrue();
        await Assert.That(configuratorFileName).IsEqualTo("RegistryConfigurator.g.cs");
        await Assert.That(shellFileName).IsEqualTo("RegistryConfigurator_Shell.g.cs");
        await Verifier.Verify(new { configuratorCode, shellCode }, verifySettings);
    }

    [Test]
    public async Task RenderRegistryConfigurator_EmptyRegistries_ReturnsFalse(CancellationToken token)
    {
        var registriesWithFactories = ImmutableArray<RegistryWithFactory>.Empty;

        var success =
            RegistryGenerator.RenderRegistryConfigurator(registriesWithFactories, BuildSettings,
                out var configuratorCode, out var configuratorFileName,
                out var shellCode, out var shellFileName);

        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task ParseResourceYaml_NullSourceText_ReturnsEmpty()
    {
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns((SourceText?)null);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, "test.sparkres.yaml", CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_SingleRegistry_SingleEntry()
    {
        var yamlContent = """
                          MinimalSampleMod.DummyRegistry.Register:
                            - test: "test.abc"
                          """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, "test.sparkres.yaml", CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.RegistryClass).IsEqualTo("MinimalSampleMod.DummyRegistry");
        await Assert.That(entry.MethodName).IsEqualTo("Register");
        await Assert.That(entry.Id).IsEqualTo("test");
        await Assert.That(entry.Files).HasSingleItem();
        await Assert.That(entry.Files.Contains((RegistryGenerator.PrimaryFileMarker, "test.abc"))).IsTrue();
    }

    [Test]
    public async Task BuildUnitsForResourceFile_SingleEntry()
    {
        var entries = ImmutableValueArray.From(
            new FileRegistrationEntry("MinimalSampleMod.DummyRegistry", "RegisterResourceFile", "hello4",
                ImmutableValueArray.From(("default", "exclusive.txt"))));

        var map = new RegistryGenerator.RegistryMap(
            new Dictionary<string, RegistryModel>
            {
                {
                    "MinimalSampleMod.DummyRegistry", new RegistryModel("DummyRegistry", "dummy", "MinimalSampleMod", false,
                        ImmutableValueArray.From(new RegisterMethodModel(
                            "RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])), [])
                }
            });

        var units = RegistryGenerator.BuildUnitsForResourceFile(entries, map);

        await Assert.That(units).HasSingleItem();
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_MultipleRegistries_MultipleEntries()
    {
        var yamlContent = """
                          TestMod.Registry1.Register:
                            - entry1:
                                config: config.json
                            - entry2:
                                data: data.xml
                          TestMod.Registry2.Register:
                            - entry3:
                                image: image.png
                          """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, "test.sparkres.yaml", CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(3);

        var entry1 = result.First(e => e.Id == "entry1");
        await Assert.That(entry1.RegistryClass).IsEqualTo("TestMod.Registry1");
        await Assert.That(entry1.MethodName).IsEqualTo("Register");
        await Assert.That(entry1.Files.Contains(("config", "config.json"))).IsTrue();

        var entry3 = result.First(e => e.Id == "entry3");
        await Assert.That(entry3.RegistryClass).IsEqualTo("TestMod.Registry2");
        await Assert.That(entry3.MethodName).IsEqualTo("Register");
        await Assert.That(entry3.Files.Contains(("image", "image.png"))).IsTrue();
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_EntryWithNoFiles()
    {
        var yamlContent = """
                          MinimalSampleMod.DummyRegistry.RegisterResourceFile:
                            - entry1:
                          """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, "test.sparkres.yaml", CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.RegistryClass).IsEqualTo("MinimalSampleMod.DummyRegistry");
        await Assert.That(entry.MethodName).IsEqualTo("RegisterResourceFile");
        await Assert.That(entry.Id).IsEqualTo("entry1");
        await Assert.That(entry.Files).HasCount().EqualTo(0);
    }

    // ── Task 2b: Full-run integration tests ──

    // Shared source snippet for marker-flagged registry + provider used in Tests 5 and 7
    private const string MarkerFlaggedRegistrySource = """
        using Sparkitect.DI.GeneratorAttributes;
        using Sparkitect.Modding;

        namespace DiTest
        {
            [Registry(Identifier = "render_pass")]
            public partial class RenderPassRegistry : global::Sparkitect.Modding.IRegistry<global::Sparkitect.Modding.TestModule>
            {
                [RegistryMethod]
                [KeyedFactoryGenerationMarkerAttribute<IRenderPass>]
                public partial void RegisterRenderPass<TRenderPass>(Identification id)
                    where TRenderPass : class, IRenderPass, IHasIdentification;
            }

            public interface IRenderPass { }

            [RenderPassRegistry.RegisterRenderPass("clear_color_pass")]
            public class ClearColorPass : IRenderPass, IHasIdentification { }
        }
        """;

    [Test]
    public async Task RegistryGenerator_FullRun_MarkerFlagged_EmitsKeyedFactoryArtifacts(CancellationToken token)
    {
        TestSources.Add(("MarkerRegistry.cs", MarkerFlaggedRegistrySource));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var fileNames = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        // Debug: list all generated files
        var allFiles = string.Join(", ", fileNames.OrderBy(f => f));

        // Artifact presence:
        //  - {Concrete}_KeyedFactory.g.cs   — Branch B, per-consumer (unchanged).
        //  - {Registry}_{Method}_KeyedFactoryConfigurator_Shell.g.cs   — Branch A, declaring
        //    assembly (here SAME compilation, so shell + registrations both land here).
        //  - {ModNs}_{Configurator}_Registrations.g.cs                  — per-consumer registrations
        //    class implementing IFactoryConfiguratorBase, carrying the now-public attribute.
        //  - {Registry}_KeyedFactoryExtensions.g.cs                     — C# 14 extension(TRegistry).
        await Assert.That(fileNames.Any(f => f == "ClearColorPass_KeyedFactory.g.cs"))
            .IsTrue().Because($"Generated files: {allFiles}");
        await Assert.That(fileNames.Any(f =>
            f == "SampleTest_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Registrations.g.cs"))
            .IsTrue().Because($"Generated files: {allFiles}");
        await Assert.That(fileNames.Any(f => f == "RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Shell.g.cs")).IsTrue();
        await Assert.That(fileNames.Any(f => f == "RenderPassRegistry_KeyedFactoryExtensions.g.cs")).IsTrue();

        // Per-consumer registrations carries the IdentificationHelper.Read<>() key expression.
        var registrationsTree2 = driverRunResult.GeneratedTrees.First(t =>
            System.IO.Path.GetFileName(t.FilePath) ==
            "SampleTest_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Registrations.g.cs");
        var configuratorCode = registrationsTree2.GetText().ToString();
        await Assert.That(configuratorCode).Contains("global::Sparkitect.Modding.IdentificationHelper.Read<global::DiTest.ClearColorPass>()");

        // The registration body now lives on the IDs struct inside a private static Register_X_Providers
        // method emitted by RegistryIdProperties.Unit.liquid; the entrypoint file (Registrations_Providers)
        // is reduced to UnsafeAccessor stubs + a dispatch ProcessRegistrations body.
        var idPropertiesTree = driverRunResult.GeneratedTrees.FirstOrDefault(t =>
            System.IO.Path.GetFileName(t.FilePath).Contains("IdProperties_Providers"));
        await Assert.That(idPropertiesTree).IsNotNull();
        var idPropertiesCode = idPropertiesTree!.GetText().ToString();
        await Assert.That(idPropertiesCode).Contains("RegisterRenderPass<global::DiTest.ClearColorPass>");

        // The entrypoint file dispatches via the UnsafeAccessor stub instead of carrying the registration line.
        var registrationsTree = driverRunResult.GeneratedTrees.FirstOrDefault(t =>
            System.IO.Path.GetFileName(t.FilePath).Contains("Registrations_Providers"));
        await Assert.That(registrationsTree).IsNotNull();
        var registrationsCode = registrationsTree!.GetText().ToString();
        await Assert.That(registrationsCode).Contains("__Reg_ClearColorPass_Providers(default, registry, IdentificationManager, ResourceManager, Scope);");

        // Assert the factory file contains expected IKeyedFactory<IRenderPass> impl
        var factoryTree = driverRunResult.GeneratedTrees.First(t =>
            System.IO.Path.GetFileName(t.FilePath) == "ClearColorPass_KeyedFactory.g.cs");
        var factoryCode = factoryTree.GetText().ToString();
        await Assert.That(factoryCode).Contains("IKeyedFactory<");

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task RegistryGenerator_FullRun_Unmarked_NoKeyedFactoryEmission(CancellationToken token)
    {
        TestSources.Add(("UnmarkedRegistry.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            namespace DiTest;

            [Registry(Identifier = "dummy")]
            public partial class DummyRegistry : IRegistry<TestModule>
            {
                [RegistryMethod]
                public partial void RegisterItem<T>(Identification id) where T : class;
            }

            [DummyRegistry.RegisterItem("my_item")]
            public class MyItem { }
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        var fileNames = driverRunResult.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .ToList();

        // No marker-driven artifacts should be emitted
        await Assert.That(fileNames.Any(f => f.Contains("KeyedFactoryConfigurator"))).IsFalse();
        await Assert.That(fileNames.Any(f => f == "MyItem_KeyedFactory.g.cs")).IsFalse();
    }

    [Test]
    public async Task MarkerConcreteProvider_IncrementalCacheable(CancellationToken token)
    {
        // Test 7 (W3 byte-equality fallback): run generator twice on same compilation,
        // assert generated file names and contents are byte-identical.
        TestSources.Add(("MarkerRegistry.cs", MarkerFlaggedRegistrySource));

        var (project, compilation) = await GetInitialCompilationAsync(token);
        var parseOptions = new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(AnalyzerConfigFiles);

        var generator = new RegistryGenerator();
        var driver = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);

        // First run
        driver = (Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out _, token);
        var firstRun = driver.GetRunResult().GeneratedTrees
            .ToDictionary(
                t => System.IO.Path.GetFileName(t.FilePath),
                t => t.GetText().ToString());

        // Second run — same driver, same compilation
        driver = (Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out _, token);
        var secondRun = driver.GetRunResult().GeneratedTrees
            .ToDictionary(
                t => System.IO.Path.GetFileName(t.FilePath),
                t => t.GetText().ToString());

        // Byte-equality: same files, same contents
        await Assert.That(firstRun.Keys.OrderBy(k => k).SequenceEqual(secondRun.Keys.OrderBy(k => k))).IsTrue();
        foreach (var key in firstRun.Keys)
        {
            await Assert.That(firstRun[key]).IsEqualTo(secondRun[key]);
        }
    }

    // Make TestAnalyzerConfigOptionsProvider accessible for Test 7
    private class TestAnalyzerConfigOptionsProvider : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider
    {
        private readonly System.Collections.Immutable.ImmutableDictionary<string, string> _globalOptions;

        public TestAnalyzerConfigOptionsProvider(List<(string Path, object Content)> analyzerConfigFiles)
        {
            var builder = System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (_, content) in analyzerConfigFiles)
            {
                if (content is string text)
                {
                    using var reader = new System.IO.StringReader(text);
                    string? line;
                    bool isGlobal = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Contains("is_global = true")) isGlobal = true;
                        else if (isGlobal && line.Contains('='))
                        {
                            var parts = line.Split(['='], 2);
                            if (parts.Length == 2) builder[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            _globalOptions = builder.ToImmutable();
        }

        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GlobalOptions =>
            new TestConfigOptions(_globalOptions);

        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(Microsoft.CodeAnalysis.SyntaxTree tree) =>
            new TestConfigOptions(System.Collections.Immutable.ImmutableDictionary<string, string>.Empty);

        public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(Microsoft.CodeAnalysis.AdditionalText textFile) =>
            new TestConfigOptions(System.Collections.Immutable.ImmutableDictionary<string, string>.Empty);

        private class TestConfigOptions : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
        {
            private readonly System.Collections.Immutable.ImmutableDictionary<string, string> _options;
            public TestConfigOptions(System.Collections.Immutable.ImmutableDictionary<string, string> options) => _options = options;
            public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value!);
            public override System.Collections.Generic.IEnumerable<string> Keys => _options.Keys;
        }
    }
}