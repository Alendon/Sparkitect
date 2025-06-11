using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
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
            build_property.ModName = TestMod
            build_property.RootNamespace = DiTest
            build_property.SgOutputNamespace = DiTest.Generated
            """));
    }

    [Test]
    public async Task RegistryGenerator_FullRun_SingleRegistry(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [Registry(Identifier = "test")]
            public class TestRegistry : IRegistry {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task RegistryGenerator_FullRun_MultipleRegistries(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest;

            [Registry(Identifier = "test1")]
            public class TestRegistry1 : IRegistry {}

            [Registry(Identifier = "test2")]
            public class TestRegistry2 : IRegistry {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

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
                public const string RegisterMethods = "RegisterItem;RegisterType";
                public const string ResourceFiles = "data:0;config:1";
                
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
        
        var resourceData = model.ResourceFiles.First(rf => rf.identifier == "data");
        await Assert.That(resourceData.optional).IsFalse();
        var resourceConfig = model.ResourceFiles.First(rf => rf.identifier == "config");
        await Assert.That(resourceConfig.optional).IsTrue();
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
            [
                new RegisterMethodModel("RegisterItem", PrimaryParameterKind.Value, TypeConstraintFlag.None, ["global::System.String"]),
                new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type, TypeConstraintFlag.ReferenceType, ["global::System.IDisposable"])
            ],
            [
                ("data", false),
                ("config", true)
            ]
        );

        var success = RegistryGenerator.RenderRegistryMetadata(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestRegistry_Metadata.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderRegistryConfigurator_MultipleRegistries(CancellationToken token)
    {
        RegistryModel[] models =
        [
            new(
                "TestRegistry1",
                "test1",
                "DiTest",
                [],
                []),
            new(
                "TestRegistry2",
                "test2",
                "DiTest.Nested",
                [],
                [])
        ];

        var settings = new ModBuildSettings("DiTest", "DiTest", false, "DiTest.Generated");

        var success = RegistryGenerator.RenderRegistryConfigurator([..models], settings, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("RegistryConfigurator.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderRegistryConfigurator_EmptyRegistries_ReturnsFalse(CancellationToken token)
    {
        var models = ImmutableArray<RegistryModel>.Empty;
        var settings = new ModBuildSettings("DiTest", "DiTest", false, "DiTest.Generated");

        var success = RegistryGenerator.RenderRegistryConfigurator(models, settings, out var code, out var fileName);

        await Assert.That(success).IsFalse();
        await Assert.That(fileName).IsEqualTo("RegistryConfigurator.g.cs");
        await Assert.That(code).IsEmpty();
    }

    #region ParseResourceYaml Tests

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
            registries:
              MinimalSampleMod.DummyRegistry_Metadata:
                - id: test
                  symbolName: TestId
                  files:
                    fileA: testA.txt
                    fileB: testB.txt
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.MetadataClass).IsEqualTo("MinimalSampleMod.DummyRegistry_Metadata");
        await Assert.That(entry.Id).IsEqualTo("test");
        await Assert.That(entry.SymbolName).IsEqualTo("TestId");
        await Assert.That(entry.Files).HasCount().EqualTo(2);
        await Assert.That(entry.Files.Contains(("fileA", "testA.txt"))).IsTrue();
        await Assert.That(entry.Files.Contains(("fileB", "testB.txt"))).IsTrue();
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_MultipleRegistries_MultipleEntries()
    {
        var yamlContent = """
            registries:
              TestMod.Registry1_Metadata:
                - id: entry1
                  symbolName: Entry1Symbol
                  files:
                    config: config.json
                - id: entry2
                  symbolName: Entry2Symbol
                  files:
                    data: data.xml
              TestMod.Registry2_Metadata:
                - id: entry3
                  symbolName: Entry3Symbol
                  files:
                    image: image.png
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(3);
        
        var entry1 = result.First(e => e.Id == "entry1");
        await Assert.That(entry1.MetadataClass).IsEqualTo("TestMod.Registry1_Metadata");
        await Assert.That(entry1.SymbolName).IsEqualTo("Entry1Symbol");
        await Assert.That(entry1.Files.Contains(("config", "config.json"))).IsTrue();

        var entry3 = result.First(e => e.Id == "entry3");
        await Assert.That(entry3.MetadataClass).IsEqualTo("TestMod.Registry2_Metadata");
        await Assert.That(entry3.Files.Contains(("image", "image.png"))).IsTrue();
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_EntryWithNoFiles()
    {
        var yamlContent = """
            registries:
              TestMod.Registry_Metadata:
                - id: entry1
                  symbolName: Entry1Symbol
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.MetadataClass).IsEqualTo("TestMod.Registry_Metadata");
        await Assert.That(entry.Id).IsEqualTo("entry1");
        await Assert.That(entry.SymbolName).IsEqualTo("Entry1Symbol");
        await Assert.That(entry.Files).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_InvalidYaml_ReturnsEmpty()
    {
        var invalidYamlContent = """
            registries:
              TestMod.Registry_Metadata:
                - id: entry1
                  symbolName: Entry1Symbol
                  invalid_yaml_structure: [unclosed_array
            """;

        var mockSourceText = SourceText.From(invalidYamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_EmptyYamlContent_ReturnsEmpty()
    {
        var emptyYamlContent = "";

        var mockSourceText = SourceText.From(emptyYamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_NullRegistries_ReturnsEmpty()
    {
        var yamlContent = """
            someOtherProperty: value
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_EmptyRegistryKey_SkipsEntry()
    {
        var yamlContent = """
            registries:
              '':
                - id: entry1
                  symbolName: Entry1Symbol
              TestMod.Registry_Metadata:
                - id: entry2
                  symbolName: Entry2Symbol
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.MetadataClass).IsEqualTo("TestMod.Registry_Metadata");
        await Assert.That(entry.Id).IsEqualTo("entry2");
    }

    [Test]
    public async Task ParseResourceYaml_EmptyEntryId_SkipsEntry()
    {
        var yamlContent = """
            registries:
              TestMod.Registry_Metadata:
                - id: ''
                  symbolName: Entry1Symbol
                - id: entry2
                  symbolName: Entry2Symbol
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.Id).IsEqualTo("entry2");
    }

    [Test]
    public async Task ParseResourceYaml_NullEntryId_SkipsEntry()
    {
        var yamlContent = """
            registries:
              TestMod.Registry_Metadata:
                - symbolName: Entry1Symbol
                - id: entry2
                  symbolName: Entry2Symbol
            """;

        var mockSourceText = SourceText.From(yamlContent);
        var mockAdditionalText = new Mock<AdditionalText>();
        mockAdditionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(mockSourceText);

        var result = RegistryGenerator.ParseResourceYaml(mockAdditionalText.Object, CancellationToken.None);

        await Assert.That(result).HasCount().EqualTo(1);
        var entry = result.First();
        await Assert.That(entry.Id).IsEqualTo("entry2");
    }

    #endregion
}