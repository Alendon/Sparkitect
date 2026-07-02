using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator;
using Sparkitect.Generator.Modding;
using VerifyTUnit;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorUnitTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);

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
    public async Task RenderRegistryAttributes_SingleFile_Snapshot()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None,
                TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("asset", true, true))
        );

        var ok = RegistryGenerator.RenderRegistryAttributes(model, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistry_Attributes.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task ErrorTypeProvider_TryExtractProviderInfo_MethodAttribute()
    {
        TestSources.Add(("ErrorTypeProvider.Modding.cs",
            """
            namespace Sparkitect.Modding
            {
                public class RegistryMethodAttribute : System.Attribute;
                public readonly struct Identification { }
            }
            """));

        TestSources.Add(("ErrorTypeProvider.Registry.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest
            {
                [Registry(Identifier = "dummy")]
                public class DummyRegistry : IRegistry<TestModule>
                {
                    [RegistryMethod]
                    public void RegisterValue(Identification id, string value) { }
                }

                public static class Providers
                {
                    [DummyRegistry.RegisterValue("hello")] // unresolved nested attribute -> error type symbol
                    public static string Value() => "x";
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        var providers = compilation.GetTypeByMetadataName("DiTest.Providers");
        var method = providers!.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "Value");
        var attr = method.GetAttributes().First();

        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regType, out var registryNamespace, out var methodName, out var isMarker);
        await Assert.That(ok).IsTrue();
        await Assert.That(regType).IsEqualTo("DummyRegistry");
        await Assert.That(registryNamespace).IsEqualTo("DiTest");
        await Assert.That(methodName).IsEqualTo("RegisterValue");
    }

    [Test]
    public async Task ErrorTypeProvider_TryExtractProviderInfo_Using()
    {
        TestSources.Add(("ErrorTypeProvider.Modding.cs",
            """
            namespace Sparkitect.Modding
            {
                public class RegistryMethodAttribute : System.Attribute;
                public readonly struct Identification { }
            }
            """));

        TestSources.Add(("ErrorTypeProvider.Registry.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            using DummyRegistryValue = DiTest.DummyRegistry.RegisterValueAttribute;

            namespace DiTest
            {
                [Registry(Identifier = "dummy")]
                public class DummyRegistry : IRegistry<TestModule>
                {
                    [RegistryMethod]
                    public void RegisterValue(Identification id, string value) { }
                }

                public static class Providers
                {
                    [DummyRegistryValue("hello")] // unresolved nested attribute -> error type symbol
                    public static string Value() => "x";
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        var providers = compilation.GetTypeByMetadataName("DiTest.Providers");
        var method = providers!.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "Value");
        var attr = method.GetAttributes().First();

        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regType,out var registryNamespace , out var methodName, out var isMarker);
        await Assert.That(ok).IsTrue();
        await Assert.That(regType).IsEqualTo("DummyRegistry");
        await Assert.That(registryNamespace).IsEqualTo("DiTest");
        await Assert.That(methodName).IsEqualTo("RegisterValue");
    }

    [Test]
    public async Task ErrorTypeProvider_MapCandidateToUnit_EndToEnd()
    {
        TestSources.Add(("ErrorTypeProvider2.Modding.cs",
            """
            namespace Sparkitect.Modding
            {
                public class RegistryMethodAttribute : System.Attribute;
                public readonly struct Identification { }
            }
            """));

        TestSources.Add(("ErrorTypeProvider2.Registry.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest
            {
                [Registry(Identifier = "dummy")]
                public class DummyRegistry : IRegistry<TestModule>
                {
                    [RegistryMethod]
                    public void RegisterValue(Identification id, string value) { }
                }

                public static class Providers
                {
                    [DummyRegistry.RegisterValue("hello")] // unresolved nested attribute -> error type symbol
                    public static string Value() => "x";
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        // Build a RegistryModel via ExtractModel
        var regType = compilation.GetTypeByMetadataName("DiTest.DummyRegistry");
        var regAttr = regType!.GetAttributes().First();
        var model = RegistryGenerator.ExtractModel((INamedTypeSymbol)regType, regAttr)!;

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(),
            ImmutableValueArray.From<RegistryModel>()));

        var candidate = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "DiTest",
            "RegisterValue",
            "hello",
            false,
            false,
            "DiTest.Providers",
            "Value",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(candidate, regMap);
        await Assert.That(unit).IsNotNull();
        await Assert.That(unit!.Model.TypeName).IsEqualTo("DummyRegistry");
        var entry = (MethodRegistrationEntry)unit.Entries.First();
        await Assert.That(entry.MethodName).IsEqualTo("RegisterValue");
        await Assert.That(entry.Id).IsEqualTo("hello");
    }

    [Test]
    public async Task ErrorTypeProvider_TryExtractProviderInfo_TypeAttribute()
    {
        TestSources.Add(("ErrorTypeProviderType.Modding.cs",
            """
            namespace Sparkitect.Modding
            {
                public class RegistryMethodAttribute : System.Attribute;
                public readonly struct Identification { }
            }
            """));

        TestSources.Add(("ErrorTypeProviderType.Registry.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;

            namespace DiTest
            {
                [Registry(Identifier = "dummy")]
                public class DummyRegistry : IRegistry<TestModule>
                {
                    [RegistryMethod]
                    public void RegisterType(Identification id) { }
                }

                [DummyRegistry.RegisterType("hello")] // unresolved nested attribute -> error type
                public class Provided {}
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);
        var provided = compilation.GetTypeByMetadataName("DiTest.Provided");
        var attr = provided!.GetAttributes().First();
        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regTypeName, out var registryNamespace, out var methodName, out _);
        await Assert.That(ok).IsTrue();
        await Assert.That(regTypeName).IsEqualTo("DummyRegistry");
        await Assert.That(registryNamespace).IsEqualTo("DiTest");
        await Assert.That(methodName).IsEqualTo("RegisterType");
    }

    [Test]
    public async Task ErrorTypeProvider_MapCandidateToUnit_Type_EndToEnd()
    {
        // Build registry and candidate
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type,
                TypeConstraintFlag.None, ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(),
            ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "DiTest",
            "RegisterType",
            "hello",
            true,
            false,
            "DiTest",
            "DiTest.Provided",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        var entry = unit!.Entries.First();
        await Assert.That(entry).IsTypeOf<TypeRegistrationEntry>();
        await Assert.That(((TypeRegistrationEntry)entry).MethodName).IsEqualTo("RegisterType");
    }

    [Test]
    public async Task Render_RegistrationsUnit_TypeEntry_Snapshot()
    {
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "MinimalSampleMod", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type,
                TypeConstraintFlag.None, ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var unit = new RegistrationUnit(model, SourceKind.Provider, "abcd",
            ImmutableValueArray.From<RegistrationEntry>(new TypeRegistrationEntry("hello3", ImmutableValueArray.From<(string, string)>(),
                "RegisterType", "global::MinimalSampleMod.RegistryExample.SampleType")));

        var ok = RegistryGenerator.RenderRegistryRegistrationsUnit(unit, BuildSettings, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistryRegistrations_Providers.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task Render_RegistrationsUnit_MixedEntries_Snapshot()
    {
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "MinimalSampleMod", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type,
                TypeConstraintFlag.None, ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var unit = new RegistrationUnit(model, SourceKind.Provider, "abcd",
            ImmutableValueArray.From<RegistrationEntry>(
                new TypeRegistrationEntry("hello3", ImmutableValueArray.From(("test1", "test.txt")),
                    "RegisterType", "global::MinimalSampleMod.RegistryExample.SampleType"),
                new MethodRegistrationEntry("hello4", ImmutableValueArray.From<(string, string)>(),
                    "RegisterObject", "global::MinimalSampleMod.RegistryExample.SampleType.GetObjectDi",
                    ImmutableValueArray.From(("global::Sparkitect.SomeType1", true), ("global::Sparkitect.SomeType2", false))),
                new MethodRegistrationEntry("hello5", ImmutableValueArray.From<(string, string)>(),
                    "RegisterObject", "global::MinimalSampleMod.RegistryExample.SampleType.SampleTypeGetObject", []),
                new PropertyRegistrationEntry("hello6", ImmutableValueArray.From<(string, string)>(),
                    "RegisterObject", "global::MinimalSampleMod.RegistryExample.SampleType.SampleTypeGetObjectProp")
            ));

        var ok = RegistryGenerator.RenderRegistryRegistrationsUnit(unit, BuildSettings, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistryRegistrations_Providers.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    // Regression: a value-providing registration method that declares DI parameters must resolve
    // them through the `scope` threaded into the generated static shim (IResolutionScope), NOT the
    // base class's `Container` instance property — the static shim cannot see instance members, so
    // emitting `Container.TryResolve` produced CS0103. This path had no consumer until a registry
    // shipped a DI-param value provider, so the stale emission slipped through the migration.
    [Test]
    public async Task MethodEntry_WithDiParameters_ResolvesThroughScope_NotContainer()
    {
        var entry = new MethodRegistrationEntry(
            "shared_image",
            ImmutableValueArray.From<(string, string)>(),
            "RegisterValue",
            "global::SampleMod.GraphImageRegistrations.Target",
            ImmutableValueArray.From(
                ("global::SampleMod.IRuntimeService", false),
                ("global::SampleMod.IOptionalDep", true)),
            "global::SampleMod.GraphImageRegistrations",
            "Target");

        var code = entry.EmitRegistrationEntryCode("registry", "MyId");

        // Never emit the out-of-scope instance property.
        await Assert.That(code.Contains("Container.TryResolve")).IsFalse();
        // Required dep: fail-fast resolve through the scope, keyed by the provider's containing type.
        await Assert.That(code).Contains(
            "if(!scope.TryResolve<global::SampleMod.IRuntimeService>(typeof(global::SampleMod.GraphImageRegistrations), out var arg_0))");
        // Optional dep: best-effort resolve, no throw.
        await Assert.That(code).Contains(
            "scope.TryResolve<global::SampleMod.IOptionalDep>(typeof(global::SampleMod.GraphImageRegistrations), out var arg_1);");
        // Provider invoked with resolved args, value handed to the registry method.
        await Assert.That(code).Contains("var value = global::SampleMod.GraphImageRegistrations.Target(arg_0, arg_1);");
        await Assert.That(code).Contains("registry.RegisterValue(MyId, value);");
    }

    [Test]
    public async Task RenderRegistryAttributes_MultiFile_GeneratesKeyedProperties()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterValue", PrimaryParameterKind.Value,
                TypeConstraintFlag.None, ImmutableValueArray.From("global::System.String"))),
            ImmutableValueArray.From(("foo", true, false), ("bar", false, false))
        );

        var ok = RegistryGenerator.RenderRegistryAttributes(model, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistry_Attributes.g.cs");
        await Assert.That(code).Contains("class RegisterValueAttribute");
        await Assert.That(code).Contains(" string FooFile ");
        await Assert.That(code).Contains(" string? BarFile ");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task MapProviderCandidateToUnit_MapsKeyedFileProps()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            false,
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])
            ),
            ImmutableValueArray.From(("texture", true, true))
        );

        var regMap = RegistryGenerator.RegistryMap.Create(([model],
            ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "MinimalSampleMod",
            "RegisterResourceFile",
            "my_id",
            false,
            false,
            "MinimalSampleMod.RegistryExample",
            "ProvideValue",
            ImmutableValueArray.From(new RegistryGenerator.ProviderFileArg("TextureFile", "foo.png")),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        await Assert.That(unit!.Model.TypeName).IsEqualTo("DummyRegistry");
        await Assert.That(unit.Entries.Count).IsEqualTo(1);
        var entry = unit.Entries.First();
        await Assert.That(entry.Files.Contains(("texture", "foo.png"))).IsTrue();
        await Assert.That(entry).IsTypeOf<MethodRegistrationEntry>();
    }

    [Test]
    public async Task MapProviderCandidateToUnit_MultiFile_MapsKeyedFileProps()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            false,
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])
            ),
            ImmutableValueArray.From(("foo", true, false), ("bar", false, false))
        );

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(),
            ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "MinimalSampleMod",
            "RegisterResourceFile",
            "my_id",
            false,
            false,
            "MinimalSampleMod.RegistryExample",
            "ProvideValue",
            ImmutableValueArray.From(
                new RegistryGenerator.ProviderFileArg("FooFile", "fileA.dat"),
                new RegistryGenerator.ProviderFileArg("BarFile", "fileB.dat")
            ),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        var files = unit!.Entries.First().Files;
        await Assert.That(files.Contains(("foo", "fileA.dat"))).IsTrue();
        await Assert.That(files.Contains(("bar", "fileB.dat"))).IsTrue();
    }

  

    [Test]
    public async Task ExtractRegisterMethods_RecognizesMarker(CancellationToken token)
    {
        TestSources.Add(("MarkerRegistry.cs",
            """
            using Sparkitect.Modding;
            namespace N;
            [Registry(Identifier = "r")]
            public class R : IRegistry<TestModule>
            {
                [RegistryMethod]
                [KeyedFactoryGenerationMarkerAttribute<IFoo>]
                public void Reg<T>(Identification id) where T : class, IFoo, IHasIdentification { }
            }
            public interface IFoo : IHasIdentification { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var rSymbol = compilation.GetTypeByMetadataName("N.R") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        await Assert.That(rSymbol).IsNotNull();

        var methods = RegistryGenerator.ExtractRegisterMethods(rSymbol!);
        await Assert.That(methods).HasSingleItem();
        await Assert.That(methods.First().KeyedFactoryMarkerTBase).IsEqualTo("global::N.IFoo");
    }

    [Test]
    public async Task ExtractRegisterMethods_UnmarkedMethod_HasNullMarker(CancellationToken token)
    {
        TestSources.Add(("UnmarkedRegistry.cs",
            """
            using Sparkitect.Modding;
            namespace N;
            [Registry(Identifier = "r")]
            public class R : IRegistry<TestModule>
            {
                [RegistryMethod]
                public void Reg<T>(Identification id) where T : class { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var rSymbol = compilation.GetTypeByMetadataName("N.R") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        await Assert.That(rSymbol).IsNotNull();

        var methods = RegistryGenerator.ExtractRegisterMethods(rSymbol!);
        await Assert.That(methods).HasSingleItem();
        await Assert.That(methods.First().KeyedFactoryMarkerTBase).IsNull();
    }

    [Test]
    public async Task MapProviderCandidate_PopulatesKeyedFactoryGenerationInfo()
    {
        var model = new RegistryModel(
            "RenderPassRegistry", "render_pass", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                "RegisterRenderPass", PrimaryParameterKind.Type,
                TypeConstraintFlag.ReferenceType,
                ImmutableValueArray.From("DiTest.IRenderPass", "Sparkitect.Modding.IHasIdentification"),
                "global::DiTest.IRenderPass")),
            ImmutableValueArray.From<(string, bool, bool)>());

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(),
            ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "RenderPassRegistry",
            "DiTest",
            "RegisterRenderPass",
            "clear_color_pass",
            true,
            false,
            "DiTest",
            "DiTest.ClearColorPass",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        var entry = unit!.Entries.First();
        await Assert.That(entry).IsTypeOf<TypeRegistrationEntry>();

        var typeEntry = (TypeRegistrationEntry)entry;
        await Assert.That(typeEntry.KeyedFactoryGeneration).IsNotNull();
        await Assert.That(typeEntry.KeyedFactoryGeneration!.TBaseFullName).IsEqualTo("global::DiTest.IRenderPass");
        await Assert.That(typeEntry.KeyedFactoryGeneration!.ConfiguratorClassName)
            .IsEqualTo("RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator");
    }

    [Test]
    public async Task MapProviderCandidate_UnmarkedTypeRegistration_KeyedFactoryGenerationIsNull()
    {
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                "RegisterType", PrimaryParameterKind.Type,
                TypeConstraintFlag.None,
                ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(),
            ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "DiTest",
            "RegisterType",
            "hello",
            true,
            false,
            "DiTest",
            "DiTest.Provided",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            []);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        var entry = unit!.Entries.First();
        await Assert.That(entry).IsTypeOf<TypeRegistrationEntry>();

        var typeEntry = (TypeRegistrationEntry)entry;
        await Assert.That(typeEntry.KeyedFactoryGeneration).IsNull();
    }

    [Test]
    public async Task RegistryMetadata_Roundtrip_PreservesMarker(CancellationToken token)
    {
        var model = new RegistryModel(
            "TestRegistry", "test", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                "RegisterRenderPass", PrimaryParameterKind.Type,
                TypeConstraintFlag.ReferenceType,
                ImmutableValueArray.From("DiTest.IRenderPass"),
                "global::DiTest.IRenderPass")),
            ImmutableValueArray.From<(string, bool, bool)>());

        var ok = RegistryGenerator.RenderRegistryMetadata(model, BuildSettings, out var code, out _);
        await Assert.That(ok).IsTrue();

        TestSources.Add(("TestMetadata.cs", code));
        var (_, compilation) = await GetInitialCompilationAsync(token);

        var metadataType = compilation.GetTypeByMetadataName("SampleTest.Generated.TestRegistry_Metadata")
            as Microsoft.CodeAnalysis.INamedTypeSymbol;
        await Assert.That(metadataType).IsNotNull();

        var success = RegistryGenerator.TryParseRegisterMethod(metadataType!, "RegisterRenderPass", out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed!.KeyedFactoryMarkerTBase).IsEqualTo("global::DiTest.IRenderPass");
    }

    [Test]
    public async Task RegistryMetadata_Roundtrip_MissingMarkerField_ParsesAsNullPreservesAllValid(CancellationToken token)
    {
        // Mirror ExtractFromMetadata_Valid pattern: hand-authored metadata omitting KeyedFactoryMarkerTBase
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
                public const string RegisterMethods = "RegisterType";
                public const string ResourceFiles = "";

                public class RegisterType {
                    public const string FunctionName = "RegisterType";
                    public const int PrimaryParameterKind = 4; // Type
                    public const int Constraint = 1; // ReferenceType
                    public const string TypeConstraint = "DiTest.ISomeBase";
                    // KeyedFactoryMarkerTBase intentionally OMITTED (pre-49.2 metadata)
                }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var metadataType = compilation.GetTypeByMetadataName("DiTest.TestMetadata")
            as Microsoft.CodeAnalysis.INamedTypeSymbol;
        await Assert.That(metadataType).IsNotNull();

        var success = RegistryGenerator.TryParseRegisterMethod(metadataType!, "RegisterType", out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.KeyedFactoryMarkerTBase).IsNull();
        await Assert.That(parsed.FunctionName).IsEqualTo("RegisterType");
        await Assert.That(parsed.PrimaryParameterKind).IsEqualTo(PrimaryParameterKind.Type);
    }

    [Test]
    public async Task Render_IdContainer_Framework_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "Minimal", false,
            ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string, bool, bool)>());

        var ok1 = RegistryGenerator.RenderRegistryIdContainer(model, BuildSettings, out var code1, out var file1);
        await Assert.That(ok1).IsTrue();
        await Assert.That(file1).IsEqualTo("DummyID.g.cs");
        await Verifier.Verify(code1, verifySettings);
    }

    [Test]
    public async Task Render_IdExtensions_Framework_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "Minimal", false,
            ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string, bool, bool)>());

        var ok2 = RegistryGenerator.RenderRegistryIdExtensionsFramework(model, BuildSettings, out var code2,
            out var file2);
        await Assert.That(ok2).IsTrue();
        await Assert.That(file2).IsEqualTo("DummyRegistry.IdFramework.g.cs");
        await Verifier.Verify(code2, verifySettings);
    }

    [Test]
    public async Task Render_RegistrationsUnit_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "MinimalSampleMod", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None,
                TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("res", true, true)));

        var unit = new RegistrationUnit(model, SourceKind.Yaml, "abcd",
            ImmutableValueArray.From<RegistrationEntry>(new ResourceRegistrationEntry("hello",
                ImmutableValueArray.From(("default", "f.txt")), "RegisterResourceFile")));

        var okR = RegistryGenerator.RenderRegistryRegistrationsUnit(unit, BuildSettings, out var codeR, out var fileR);
        await Assert.That(okR).IsTrue();
        await Assert.That(fileR).IsEqualTo("DummyRegistryRegistrations_Resources.g.cs");
        await Assert.That(codeR).IsNotEmpty();

        await Verifier.Verify(codeR, verifySettings);
    }

    [Test]
    public async Task Render_IdPropertiesUnit_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "MinimalSampleMod", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None,
                TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("res", true, true)));

        var unit = new RegistrationUnit(model, SourceKind.Yaml, "abcd",
            ImmutableValueArray.From<RegistrationEntry>(new ResourceRegistrationEntry("hello",
                ImmutableValueArray.From(("default", "f.txt")), "RegisterResourceFile")));

        var okP = RegistryGenerator.RenderRegistryIdPropertiesUnit(unit, BuildSettings, out var codeP, out var fileP);
        await Assert.That(okP).IsTrue();
        await Assert.That(fileP).IsEqualTo("DummyRegistry.IdProperties_Resources.g.cs");
        await Verifier.Verify(codeP, verifySettings);
    }

    // Task 2a tests

    private static RegistrationUnit BuildMarkerFlaggedUnit(
        string registryName = "RenderPassRegistry",
        string methodName = "RegisterRenderPass",
        string tBaseFullName = "global::DiTest.IRenderPass",
        params (string id, string typeFullName)[] entries)
    {
        var configuratorClassName = $"{registryName}_{methodName}_KeyedFactoryConfigurator";
        var kfg = new KeyedFactoryGenerationInfo(tBaseFullName, configuratorClassName);

        var model = new RegistryModel(
            registryName, "render_pass", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                methodName, PrimaryParameterKind.Type, TypeConstraintFlag.ReferenceType,
                ImmutableValueArray.From("DiTest.IRenderPass"), tBaseFullName)),
            ImmutableValueArray.From<(string, bool, bool)>());

        var registrationEntries = entries.Select(e =>
            (RegistrationEntry)new TypeRegistrationEntry(
                e.id,
                ImmutableValueArray.From<(string, string)>(),
                methodName,
                e.typeFullName,
                kfg)).ToArray();

        return new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From(registrationEntries));
    }

    [Test]
    public async Task RenderKeyedFactoryRegistrations_SingleMarker_Snapshot()
    {
        var unit = BuildMarkerFlaggedUnit(
            entries: [("clear_color_pass", "global::DiTest.ClearColorPass")]);

        var groups = RegistryGenerator.RenderKeyedFactoryRegistrations(unit, BuildSettings);
        await Assert.That(groups).HasSingleItem();

        var group = groups[0];
        // Per-consumer registrations class — non-partial, internal sealed, prefixed with {ModId}Pascal.
        await Assert.That(group.FileName)
            .IsEqualTo("SampleTest_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Registrations.g.cs");
        await Assert.That(group.Code).Contains(
            "internal sealed class SampleTest_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Registrations");
        await Assert.That(group.Code).Contains(
            ": global::Sparkitect.DI.IFactoryConfiguratorBase<global::Sparkitect.Modding.Identification, global::DiTest.IRenderPass>");
        await Assert.That(group.Code).Contains(
            "[global::SampleTest.Generated.RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfiguratorAttribute]");
        await Assert.That(group.Code).Contains(
            "registrations[global::Sparkitect.Modding.IdentificationHelper.Read<global::DiTest.ClearColorPass>()] = new global::DiTest.ClearColorPass_KeyedFactory();");

        await Verifier.Verify(group.Code, verifySettings);
    }

    [Test]
    public async Task RenderKeyedFactoryRegistrations_MixedMarkedAndUnmarked()
    {
        // Unit with one marker-flagged + one unmarked entry — only the marked entry should
        // contribute to per-consumer registrations emission.
        var configuratorClassName = "RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator";
        var kfg = new KeyedFactoryGenerationInfo("global::DiTest.IRenderPass", configuratorClassName);

        var model = new RegistryModel(
            "RenderPassRegistry", "render_pass", "DiTest", false,
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterRenderPass", PrimaryParameterKind.Type,
                    TypeConstraintFlag.ReferenceType, ImmutableValueArray.From("DiTest.IRenderPass"),
                    "global::DiTest.IRenderPass"),
                new RegisterMethodModel("RegisterOther", PrimaryParameterKind.Type,
                    TypeConstraintFlag.ReferenceType, ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var entries = ImmutableValueArray.From<RegistrationEntry>(
            new TypeRegistrationEntry("clear_color_pass", ImmutableValueArray.From<(string, string)>(),
                "RegisterRenderPass", "global::DiTest.ClearColorPass", kfg),
            new TypeRegistrationEntry("other_entry", ImmutableValueArray.From<(string, string)>(),
                "RegisterOther", "global::DiTest.OtherType", null));

        var unit = new RegistrationUnit(model, SourceKind.Provider, "Providers", entries);

        var groups = RegistryGenerator.RenderKeyedFactoryRegistrations(unit, BuildSettings);
        await Assert.That(groups).HasSingleItem();

        var group = groups[0];
        await Assert.That(group.Code).Contains("global::DiTest.ClearColorPass");
        await Assert.That(group.Code).DoesNotContain("OtherType");

        await Verifier.Verify(group.Code, verifySettings);
    }

    [Test]
    public async Task RenderKeyedFactoryRegistrations_MultipleProvidersOneMarkerMethod()
    {
        var unit = BuildMarkerFlaggedUnit(
            entries: [
                ("clear_color_pass", "global::DiTest.ClearColorPass"),
                ("blur_pass", "global::DiTest.BlurPass")
            ]);

        var groups = RegistryGenerator.RenderKeyedFactoryRegistrations(unit, BuildSettings);
        await Assert.That(groups).HasSingleItem();

        var group = groups[0];
        await Assert.That(group.Code).Contains("global::DiTest.ClearColorPass");
        await Assert.That(group.Code).Contains("global::DiTest.BlurPass");
        await Assert.That(group.Code).Contains(
            "internal sealed class SampleTest_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Registrations");

        await Verifier.Verify(group.Code, verifySettings);
    }

    [Test]
    public async Task KeyedFactory_Code_Uses_IdentificationHelper_Read()
    {
        var unit = BuildMarkerFlaggedUnit(
            entries: [("clear_color_pass", "global::DiTest.ClearColorPass")]);

        var groups = RegistryGenerator.RenderKeyedFactoryRegistrations(unit, BuildSettings);
        await Assert.That(groups).HasSingleItem();

        var registrationsCode = groups[0].Code;
        await Assert.That(registrationsCode).Contains(
            "registrations[global::Sparkitect.Modding.IdentificationHelper.Read<global::DiTest.ClearColorPass>()] = new global::DiTest.ClearColorPass_KeyedFactory();");
    }

    [Test]
    public async Task GenerateKeyedFactoryConfiguratorShell_PublicSealed_Snapshot()
    {
        var code = RegistryGenerator.GenerateKeyedFactoryConfiguratorShell(
            "SampleTest.Generated",
            "RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator");

        // Shell + attribute must be `public sealed` so consumers across assemblies can typeof() the attribute.
        await Assert.That(code).Contains(
            "public sealed class RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfiguratorAttribute : global::System.Attribute");
        await Assert.That(code).Contains(
            "public sealed class RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator");
        // Shell no longer carries a Configure body or IFactoryConfigurator interface — that lives
        // on the per-consumer registrations class.
        await Assert.That(code).DoesNotContain("IFactoryConfigurator");
        await Assert.That(code).DoesNotContain("Configure(");

        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task GenerateKeyedFactoryExtensions_TypeGetterAndBuilder_Snapshot()
    {
        var registry = new RegistryModel(
            "RenderPassRegistry", "render_pass", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                "RegisterRenderPass", PrimaryParameterKind.Type, TypeConstraintFlag.ReferenceType,
                ImmutableValueArray.From("DiTest.IRenderPass"),
                "global::DiTest.IRenderPass",
                "global::Sparkitect.Modding.Identification")),
            ImmutableValueArray.From<(string, bool, bool)>());

        var code = RegistryGenerator.GenerateKeyedFactoryExtensions(
            "SampleTest.Generated", registry,
            registry.RegisterMethods.ToArray());

        await Assert.That(code).Contains("namespace SampleTest.Generated.KeyedFactoryExtensions");
        await Assert.That(code).Contains(
            "public static class RenderPassRegistryKeyedFactoryExtensions");
        await Assert.That(code).Contains(
            "extension(global::DiTest.RenderPassRegistry)");
        await Assert.That(code).Contains(
            "public static global::System.Type RegisterRenderPassConfiguratorAttribute");
        await Assert.That(code).Contains(
            "public static global::Sparkitect.DI.Container.IFactoryContainer<global::Sparkitect.Modding.Identification, global::DiTest.IRenderPass> BuildRegisterRenderPassContainer(");

        await Verifier.Verify(code, verifySettings);
    }

    // ── Phase 49.3 (D-19) — auto-emit IHasIdentification snapshot tests ──

    /// <summary>
    /// Builds an unmarked TypeRegistrationEntry-only unit (no <see cref="KeyedFactoryGenerationInfo"/>).
    /// Used for the 49.3 auto-emit snapshot tests where keyed-factory generation does not apply.
    /// </summary>
    private static RegistrationUnit BuildTypeRegistrationUnit(
        string registryName = "RenderPassRegistry",
        string methodName = "RegisterRenderPass",
        params (string id, string typeFullName)[] entries)
    {
        var model = new RegistryModel(
            registryName, "render_pass", "DiTest", false,
            ImmutableValueArray.From(new RegisterMethodModel(
                methodName, PrimaryParameterKind.Type, TypeConstraintFlag.ReferenceType,
                ImmutableValueArray.From<string>())),
            ImmutableValueArray.From<(string, bool, bool)>());

        var registrationEntries = entries.Select(e =>
            (RegistrationEntry)new TypeRegistrationEntry(
                e.id,
                ImmutableValueArray.From<(string, string)>(),
                methodName,
                e.typeFullName)).ToArray();

        return new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From(registrationEntries));
    }

    [Test]
    public async Task RenderAutoEmitIdentification_SingleTypeRegistration_Snapshot()
    {
        var unit = BuildTypeRegistrationUnit(
            entries: [("clear_color_pass", "global::DiTest.ClearColorPass")]);

        var ok = RegistryGenerator.RenderAutoEmitIdentificationUnit(unit, BuildSettings, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("RenderPassRegistry.AutoEmitIdentification_Providers.g.cs");

        // Substring guards before snapshot acceptance:
        // Block-style namespace (NOT file-scoped) — multiple per-concrete partial declarations
        // may live in distinct namespaces in a single emission file, and C# allows only one
        // file-scoped namespace per .cs file.
        await Assert.That(code).Contains("namespace DiTest");
        // D-12: auto-emit no longer emits the ': IHasIdentification' base-list; the interface
        // must be declared in user source. The static Identification member is still emitted.
        await Assert.That(code).Contains("partial class ClearColorPass");
        await Assert.That(code).DoesNotContain(": global::Sparkitect.Modding.IHasIdentification");
        await Assert.That(code).Contains("public static global::Sparkitect.Modding.Identification Identification");
        // The auto-emitted IHasIdentification reads through the C# 14 extension chain
        // (IDs.{Cat}ID.{Mod}.{PropertyName}) instead of the entrypoint's static field — the
        // entrypoint no longer holds per-entry storage.
        await Assert.That(code).Contains("global::Sparkitect.Modding.IDs.RenderPassID.SampleTest.ClearColorPass");

        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderAutoEmitIdentification_NoTypeEntries_ProducesNoOutput()
    {
        // Pitfall 5 (RESEARCH): only TypeRegistrationEntry triggers auto-emit; value/method/property
        // providers are different RegistrationEntry subtypes and never carry IHasIdentification.
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "MinimalSampleMod", false,
            ImmutableValueArray.From(new RegisterMethodModel("RegisterValue", PrimaryParameterKind.Value,
                TypeConstraintFlag.None, ImmutableValueArray.From("string"))),
            ImmutableValueArray.From<(string, bool, bool)>());

        var unit = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From<RegistrationEntry>(
                new MethodRegistrationEntry(
                    "hello", ImmutableValueArray.From<(string, string)>(),
                    "RegisterValue", "global::MinimalSampleMod.DummyValueProvider.GetHello",
                    ImmutableValueArray.From<(string, bool)>())));

        var ok = RegistryGenerator.RenderAutoEmitIdentificationUnit(unit, BuildSettings, out var code, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(code).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RenderAutoEmitIdentification_MarkerFlaggedConcrete_BothArtifactsEmit()
    {
        // Two orthogonal emission paths (auto-emit Identification member and keyed-factory
        // per-consumer registrations) coexist on the same marker-flagged concrete.
        var unit = BuildMarkerFlaggedUnit(
            entries: [("clear_color_pass", "global::DiTest.ClearColorPass")]);

        // Keyed-factory per-consumer registrations artifact:
        var kfGroups = RegistryGenerator.RenderKeyedFactoryRegistrations(unit, BuildSettings);
        await Assert.That(kfGroups.Length).IsGreaterThanOrEqualTo(1);

        // Auto-emit Identification-member artifact:
        var autoOk = RegistryGenerator.RenderAutoEmitIdentificationUnit(unit, BuildSettings, out var autoCode, out _);
        await Assert.That(autoOk).IsTrue();
        // D-12: base-list dropped from auto-emit; the Identification member is still emitted.
        await Assert.That(autoCode).Contains("partial class ClearColorPass");
        await Assert.That(autoCode).DoesNotContain(": global::Sparkitect.Modding.IHasIdentification");

        await Verifier.Verify(new { autoCode, kfFirstRegistrations = kfGroups[0].Code }, verifySettings);
    }
}