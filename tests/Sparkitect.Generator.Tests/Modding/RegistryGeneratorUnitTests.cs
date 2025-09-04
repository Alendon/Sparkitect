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
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

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
    public async Task ComputeStableTag_IsStable_AndPathSensitive()
    {
        var a1 = RegistryGenerator.ComputeStableTag("/a.cs", 10);
        var a2 = RegistryGenerator.ComputeStableTag("/a.cs", 10);
        var b = RegistryGenerator.ComputeStableTag("/b.cs", 10);
        var c = RegistryGenerator.ComputeStableTag("/a.cs", 11);

        await Assert.That(a1).IsEqualTo(a2);
        await Assert.That(a1).IsNotEqualTo(b);
        await Assert.That(a1).IsNotEqualTo(c);
    }

    [Test]
    public async Task RenderRegistryAttributes_SingleFile_Snapshot()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("asset", true))
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
                public class DummyRegistry : IRegistry
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

        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regType, out var methodName, out var isMarker);
        await Assert.That(ok).IsTrue();
        await Assert.That(regType).IsEqualTo("DummyRegistry");
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
                public class DummyRegistry : IRegistry
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

        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regType, out var methodName, out var isMarker);
        await Assert.That(ok).IsTrue();
        await Assert.That(regType).IsEqualTo("DummyRegistry");
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
                public class DummyRegistry : IRegistry
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

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(), ImmutableValueArray.From<RegistryModel>()));

        var candidate = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "RegisterValue",
            "hello",
            false,
            "DiTest.Providers",
            "Value",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            "/src/p.cs",
            5);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(candidate, regMap);
        await Assert.That(unit).IsNotNull();
        await Assert.That(unit!.Model.TypeName).IsEqualTo("DummyRegistry");
        await Assert.That(unit.Entries.First().MethodName).IsEqualTo("RegisterValue");
        await Assert.That(unit.Entries.First().Id).IsEqualTo("hello");
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
                public class DummyRegistry : IRegistry
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
        var ok = RegistryGenerator.TryExtractProviderInfo(attr, out var regTypeName, out var methodName, out _);
        await Assert.That(ok).IsTrue();
        await Assert.That(regTypeName).IsEqualTo("DummyRegistry");
        await Assert.That(methodName).IsEqualTo("RegisterType");
    }

    [Test]
    public async Task ErrorTypeProvider_MapCandidateToUnit_Type_EndToEnd()
    {
        // Build registry and candidate
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "DiTest",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type, TypeConstraintFlag.None, ImmutableValueArray.From<string>() )),
            ImmutableValueArray.From<(string,bool)>());

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(), ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "RegisterType",
            "hello",
            true,
            "DiTest",
            "DiTest.Provided",
            new ImmutableValueArray<RegistryGenerator.ProviderFileArg>.Builder().ToImmutableValueArray(),
            "/p.cs",
            1);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        await Assert.That(unit!.Entries.First().Kind).IsEqualTo(EntryKind.Type);
        await Assert.That(unit.Entries.First().MethodName).IsEqualTo("RegisterType");
    }

    [Test]
    public async Task Render_RegistrationsUnit_TypeEntry_Snapshot()
    {
        var model = new RegistryModel(
            "DummyRegistry", "dummy", "MinimalSampleMod",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterType", PrimaryParameterKind.Type, TypeConstraintFlag.None, ImmutableValueArray.From<string>() )),
            ImmutableValueArray.From<(string,bool)>());

        var unit = new RegistrationUnit(model, SourceKind.Provider, "abcd",
            ImmutableValueArray.From(new RegistrationEntry("hello3", EntryKind.Type, "RegisterType", string.Empty, "MinimalSampleMod.RegistryExample.SampleType", ImmutableValueArray.From<(string,string)>())));

        var ok = RegistryGenerator.RenderRegistryRegistrationsUnit(unit, BuildSettings, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistryRegistrations_abcd.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderRegistryAttributes_MultiFile_GeneratesKeyedProperties()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterValue", PrimaryParameterKind.Value, TypeConstraintFlag.None, ImmutableValueArray.From("global::System.String"))),
            ImmutableValueArray.From(("foo", false), ("bar", true))
        );

        var ok = RegistryGenerator.RenderRegistryAttributes(model, out var code, out var file);
        await Assert.That(ok).IsTrue();
        await Assert.That(file).IsEqualTo("DummyRegistry_Attributes.g.cs");
        await Assert.That(code).Contains("class RegisterValueAttribute");
        await Assert.That(code).Contains(" string Foo ");
        await Assert.That(code).Contains(" string? Bar ");
    }

    [Test]
    public async Task MapProviderCandidateToUnit_SingleFile_MapsFileToDefault()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])
            ),
            ImmutableValueArray.From(("texture", false))
        );

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(), ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "RegisterResourceFile",
            "my_id",
            false,
            "MinimalSampleMod.RegistryExample",
            "ProvideValue",
            ImmutableValueArray.From(new RegistryGenerator.ProviderFileArg("File", "foo.png")),
            "/src/a.cs",
            10);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        await Assert.That(unit!.Model.TypeName).IsEqualTo("DummyRegistry");
        await Assert.That(unit.Entries.Count).IsEqualTo(1);
        var entry = unit.Entries.First();
        await Assert.That(entry.Files.Contains(("default", "foo.png"))).IsTrue();
        await Assert.That(entry.Kind).IsEqualTo(EntryKind.Method);
    }

    [Test]
    public async Task MapProviderCandidateToUnit_MultiFile_MapsPascalPropsToIds()
    {
        var model = new RegistryModel(
            "DummyRegistry",
            "dummy",
            "MinimalSampleMod",
            ImmutableValueArray.From(
                new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])
            ),
            ImmutableValueArray.From(("foo", false), ("bar", true))
        );

        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model }.ToImmutableArray(), ImmutableValueArray.From<RegistryModel>()));

        var cand = new RegistryGenerator.ProviderCandidate(
            "DummyRegistry",
            "RegisterResourceFile",
            "my_id",
            false,
            "MinimalSampleMod.RegistryExample",
            "ProvideValue",
            ImmutableValueArray.From(
                new RegistryGenerator.ProviderFileArg("Foo", "fileA.dat"),
                new RegistryGenerator.ProviderFileArg("Bar", "fileB.dat")
            ),
            "/src/a.cs",
            20);

        var unit = RegistryGenerator.MapProviderCandidateToUnit(cand, regMap);
        await Assert.That(unit).IsNotNull();
        var files = unit!.Entries.First().Files;
        await Assert.That(files.Contains(("foo", "fileA.dat"))).IsTrue();
        await Assert.That(files.Contains(("bar", "fileB.dat"))).IsTrue();
    }

    [Test]
    public async Task BuildUnitsForResourceFile_GroupsPerRegistryAndSorts()
    {
        var model1 = new RegistryModel("Reg1", "cat1", "NS1", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());
        var model2 = new RegistryModel("Reg2", "cat2", "NS2", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());
        var regMap = RegistryGenerator.RegistryMap.Create((new[] { model1, model2 }.ToImmutableArray(), ImmutableValueArray.From<RegistryModel>()));

        var entries = ImmutableValueArray.From(
            new FileRegistrationEntry("NS1.Reg1_Metadata", "b", ImmutableValueArray.From(("k","f2"))),
            new FileRegistrationEntry("NS1.Reg1_Metadata", "a", ImmutableValueArray.From(("k","f1"))),
            new FileRegistrationEntry("NS2.Reg2_Metadata", "c", ImmutableValueArray.From<(string,string)>())
        );

        var units = RegistryGenerator.BuildUnitsForResourceFile("/data.yaml", entries, regMap);
        await Assert.That(units.Count).IsEqualTo(2);
        var u1 = units.First(u => u.Model.TypeName == "Reg1");
        await Assert.That(u1.Entries.First().Id).IsEqualTo("a");
        await Assert.That(u1.Entries.Last().Id).IsEqualTo("b");
        var u2 = units.First(u => u.Model.TypeName == "Reg2");
        await Assert.That(u2.Entries.Single().Id).IsEqualTo("c");
    }

    [Test]
    public async Task Render_IdContainer_Framework_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "Minimal", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());

        var ok1 = RegistryGenerator.RenderRegistryIdContainer(model, BuildSettings, out var code1, out var file1);
        await Assert.That(ok1).IsTrue();
        await Assert.That(file1).IsEqualTo("DummyID.g.cs");
        await Verifier.Verify(code1, verifySettings);
    }

    [Test]
    public async Task Render_IdExtensions_Framework_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "Minimal", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());

        var ok2 = RegistryGenerator.RenderRegistryIdExtensionsFramework(model, BuildSettings, out var code2, out var file2);
        await Assert.That(ok2).IsTrue();
        await Assert.That(file2).IsEqualTo("DummyRegistry.IdFramework.g.cs");
        await Verifier.Verify(code2, verifySettings);
    }

    [Test]
    public async Task Render_RegistrationsUnit_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "MinimalSampleMod",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("res", false)));

        var unit = new RegistrationUnit(model, SourceKind.Yaml, "abcd",
            ImmutableValueArray.From(new RegistrationEntry("hello", EntryKind.Resource, "RegisterResourceFile", string.Empty, string.Empty,
                ImmutableValueArray.From(("default", "f.txt")) )));

        var okR = RegistryGenerator.RenderRegistryRegistrationsUnit(unit, BuildSettings, out var codeR, out var fileR);
        await Assert.That(okR).IsTrue();
        await Assert.That(fileR).IsEqualTo("DummyRegistryRegistrations_abcd.g.cs");
        await Assert.That(codeR).IsNotEmpty();

        await Verifier.Verify(codeR, verifySettings);
    }

    [Test]
    public async Task Render_IdPropertiesUnit_Snapshot()
    {
        var model = new RegistryModel("DummyRegistry", "dummy", "MinimalSampleMod",
            ImmutableValueArray.From(new RegisterMethodModel("RegisterResourceFile", PrimaryParameterKind.None, TypeConstraintFlag.None, [])),
            ImmutableValueArray.From(("res", false)));

        var unit = new RegistrationUnit(model, SourceKind.Yaml, "abcd",
            ImmutableValueArray.From(new RegistrationEntry("hello", EntryKind.Resource, "RegisterResourceFile", string.Empty, string.Empty,
                ImmutableValueArray.From(("default", "f.txt")) )));

        var okP = RegistryGenerator.RenderRegistryIdPropertiesUnit(unit, BuildSettings, out var codeP, out var fileP);
        await Assert.That(okP).IsTrue();
        await Assert.That(fileP).IsEqualTo("DummyRegistry.IdProperties_abcd.g.cs");
        await Verifier.Verify(codeP, verifySettings);
    }
}
