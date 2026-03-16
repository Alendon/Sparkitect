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
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

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

        var success = RegistryGenerator.RenderRegistryMetadata(model, out var code, out var fileName);

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
                    new RegistrationModel("global::DiTest.TestRegistry1_KeyedFactory", [])),
                []),
            new(
                new RegistryModel("TestRegistry2", "test2", "DiTest.Nested", false, [], []),
                new FactoryWithRegistration(
                    new FactoryModel("Sparkitect.Modding.IRegistryBase", "TestRegistry2", "DiTest.Nested", [], [],
                        new FactoryIntent.Keyed("test2"), []),
                    new RegistrationModel("global::DiTest.Nested.TestRegistry2_KeyedFactory", [])), [])
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

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

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

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

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

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

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

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.RegistryClass).IsEqualTo("MinimalSampleMod.DummyRegistry");
        await Assert.That(entry.MethodName).IsEqualTo("RegisterResourceFile");
        await Assert.That(entry.Id).IsEqualTo("entry1");
        await Assert.That(entry.Files).HasCount().EqualTo(0);
    }
}