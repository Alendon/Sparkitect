using System.Threading;
using System.Threading.Tasks;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Generator.GameState;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.DI;

public class DiPipelineTests : SourceGeneratorTestBase<StateModuleServiceGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.DiPipelineAttributes);
        TestSources.Add(TestData.DiContainerInterfaces);
    }

    public override ModBuildSettings BuildSettings { get; }

    // ── ExtractFactory Tests ────────────────────────────────────────────

    [Test]
    public async Task ExtractFactory_ParameterlessConstructor_ReturnsModelWithEmptyArgs(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IMyService { }
            public class MyService : IMyService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.MyService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IMyService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ConstructorArguments).HasCount().EqualTo(0);
        await Assert.That(result.RequiredProperties).HasCount().EqualTo(0);
        await Assert.That(result.ImplementationTypeName).IsEqualTo("MyService");
        await Assert.That(result.ImplementationNamespace).IsEqualTo("DiPipelineTest");
        await Assert.That(result.Intent).IsTypeOf<FactoryIntent.Service>();
        await Assert.That(result.BaseType).IsEqualTo("IMyService");
    }

    [Test]
    public async Task ExtractFactory_WithConstructorArgs_ReturnsModelWithArgs(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface ILogger { }
            public interface IConfig { }
            public interface IMyServiceWithArgs { }
            public class MyServiceWithArgs : IMyServiceWithArgs
            {
                public MyServiceWithArgs(DiPipelineTest.ILogger logger, DiPipelineTest.IConfig config) { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.MyServiceWithArgs");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IMyServiceWithArgs");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ConstructorArguments).HasCount().EqualTo(2);
        await Assert.That(result.ConstructorArguments[0].Type).IsEqualTo("DiPipelineTest.ILogger");
        await Assert.That(result.ConstructorArguments[1].Type).IsEqualTo("DiPipelineTest.IConfig");
        await Assert.That(result.ConstructorArguments[0].IsOptional).IsFalse();
        await Assert.That(result.ConstructorArguments[1].IsOptional).IsFalse();
    }

    [Test]
    public async Task ExtractFactory_WithNullableArg_MarksAsOptional(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface INullableLogger { }
            public interface INullableService { }
            public class NullableArgService : INullableService
            {
                public NullableArgService(DiPipelineTest.INullableLogger? logger) { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.NullableArgService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "INullableService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ConstructorArguments).HasCount().EqualTo(1);
        await Assert.That(result.ConstructorArguments[0].IsOptional).IsTrue();
    }

    [Test]
    public async Task ExtractFactory_WithRequiredProperty_IncludesProperty(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IRenderer { }
            public interface IPropertyService { }
            public class PropertyService : IPropertyService
            {
                public required DiPipelineTest.IRenderer Renderer { get; set; }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.PropertyService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IPropertyService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RequiredProperties).HasCount().EqualTo(1);
        await Assert.That(result.RequiredProperties[0].Type).IsEqualTo("DiPipelineTest.IRenderer");
        await Assert.That(result.RequiredProperties[0].SetterName).IsEqualTo("set_Renderer");
        await Assert.That(result.RequiredProperties[0].IsOptional).IsFalse();
    }

    [Test]
    public async Task ExtractFactory_InheritedRequiredProperty_IncludesFromBase(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IBaseDep { }
            public interface IInheritedService { }
            public class BaseClass
            {
                public required DiPipelineTest.IBaseDep BaseDep { get; set; }
            }
            public class InheritedService : BaseClass, IInheritedService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.InheritedService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IInheritedService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RequiredProperties).HasCount().EqualTo(1);
        await Assert.That(result.RequiredProperties[0].Type).IsEqualTo("DiPipelineTest.IBaseDep");
    }

    [Test]
    public async Task ExtractFactory_KeyedIntent_SetsKeyCorrectly(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IKeyedService { }
            public class KeyedService : IKeyedService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.KeyedService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Keyed("my_key"), "IKeyedService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Intent).IsTypeOf<FactoryIntent.Keyed>();
        var keyed = (FactoryIntent.Keyed)result.Intent;
        await Assert.That(keyed.Key).IsEqualTo("my_key");
    }

    [Test]
    public async Task ExtractFactory_WithOptionalModDependent_ExtractsModIds(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            using Sparkitect.Modding;

            namespace DiPipelineTest;

            public interface IModService { }

            [OptionalModDependent("color_mod")]
            public class ModService : IModService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.ModService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IModService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OptionalModIds).HasCount().EqualTo(1);
        await Assert.That(result.OptionalModIds[0]).IsEqualTo("color_mod");
    }

    [Test]
    public async Task ExtractFactory_MultipleOptionalModDependent_ExtractsAllModIds(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            using Sparkitect.Modding;

            namespace DiPipelineTest;

            public interface IMultiModService { }

            [OptionalModDependent("mod_a")]
            [OptionalModDependent("mod_b")]
            public class MultiModService : IMultiModService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.MultiModService");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractFactory(type!, new FactoryIntent.Service(), "IMultiModService");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OptionalModIds).HasCount().EqualTo(2);
    }

    // ── ExtractConditionalModIds Tests ──────────────────────────────────

    [Test]
    public async Task ExtractConditionalModIds_NoAttributes_ReturnsEmpty(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public class PlainClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.PlainClass");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractConditionalModIds(type!);

        await Assert.That(result).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ExtractConditionalModIds_SingleAttribute_ReturnsSingleId(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            using Sparkitect.Modding;

            namespace DiPipelineTest;

            [OptionalModDependent("color_mod")]
            public class SingleModClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.SingleModClass");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractConditionalModIds(type!);

        await Assert.That(result).HasCount().EqualTo(1);
        await Assert.That(result[0]).IsEqualTo("color_mod");
    }

    [Test]
    public async Task ExtractConditionalModIds_MultipleAttributes_ReturnsAllIds(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            using Sparkitect.Modding;

            namespace DiPipelineTest;

            [OptionalModDependent("mod_a")]
            [OptionalModDependent("mod_b")]
            public class MultiModClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.MultiModClass");
        await Assert.That(type).IsNotNull();

        var result = DiPipeline.ExtractConditionalModIds(type!);

        await Assert.That(result).HasCount().EqualTo(2);
    }

    // ── RenderFactory Tests (snapshot-verified) ─────────────────────────

    [Test]
    public async Task RenderFactory_ServiceIntent_NoArgs_RendersFactory(CancellationToken token)
    {
        var model = new FactoryModel(
            BaseType: "IMyService",
            ImplementationTypeName: "MyService",
            ImplementationNamespace: "TestNamespace",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Service(),
            OptionalModIds: []);

        var success = DiPipeline.RenderFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("MyService_Factory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderFactory_ServiceIntent_WithArgsAndProperties_RendersFactory(CancellationToken token)
    {
        var model = new FactoryModel(
            BaseType: "IMyService",
            ImplementationTypeName: "MyService",
            ImplementationNamespace: "TestNamespace",
            ConstructorArguments: ImmutableValueArray.From(
                new ConstructorArgument("TestNamespace.ILogger", false),
                new ConstructorArgument("TestNamespace.IConfig?", true)),
            RequiredProperties: ImmutableValueArray.From(
                new RequiredProperty("TestNamespace.IRenderer", "set_Renderer", false, "global::TestNamespace.MyService")),
            Intent: new FactoryIntent.Service(),
            OptionalModIds: []);

        var success = DiPipeline.RenderFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("MyService_Factory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderFactory_KeyedIntent_RendersKeyedFactory(CancellationToken token)
    {
        var model = new FactoryModel(
            BaseType: "Sparkitect.Modding.IRegistryBase",
            ImplementationTypeName: "TestRegistry",
            ImplementationNamespace: "TestNamespace",
            ConstructorArguments: ImmutableValueArray.From(
                new ConstructorArgument("TestNamespace.ILogger", false)),
            RequiredProperties: [],
            Intent: new FactoryIntent.Keyed("test_key"),
            OptionalModIds: []);

        var success = DiPipeline.RenderFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestRegistry_KeyedFactory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderFactory_WithOptionalModIds_RendersAnnotations(CancellationToken token)
    {
        var model = new FactoryModel(
            BaseType: "IMyService",
            ImplementationTypeName: "ModAnnotatedService",
            ImplementationNamespace: "TestNamespace",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Service(),
            OptionalModIds: ImmutableValueArray.From("color_mod"));

        var success = DiPipeline.RenderFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    // ── ToRegistration Tests (Roslyn-based) ─────────────────────────────

    [Test]
    public async Task ToRegistration_ServiceIntent_GeneratesCorrectFactoryTypeName(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IRegService { }
            public class RegService : IRegService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.RegService");
        await Assert.That(type).IsNotNull();

        var factory = new FactoryModel(
            BaseType: "IRegService",
            ImplementationTypeName: "RegService",
            ImplementationNamespace: "DiPipelineTest",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Service(),
            OptionalModIds: []);

        var result = DiPipeline.ToRegistration(factory, type!);

        await Assert.That(result.FactoryTypeName).IsEqualTo("global::DiPipelineTest.RegService_Factory");
    }

    [Test]
    public async Task ToRegistration_KeyedIntent_GeneratesCorrectFactoryTypeName(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface IKeyedRegService { }
            public class KeyedRegService : IKeyedRegService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.KeyedRegService");
        await Assert.That(type).IsNotNull();

        var factory = new FactoryModel(
            BaseType: "IKeyedRegService",
            ImplementationTypeName: "KeyedRegService",
            ImplementationNamespace: "DiPipelineTest",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Keyed("test_key"),
            OptionalModIds: []);

        var result = DiPipeline.ToRegistration(factory, type!);

        await Assert.That(result.FactoryTypeName).IsEqualTo("global::DiPipelineTest.KeyedRegService_KeyedFactory");
    }

    [Test]
    public async Task ToRegistration_WithConditionalModIds_ExtractsFromSymbol(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            using Sparkitect.Modding;

            namespace DiPipelineTest;

            public interface ICondService { }

            [OptionalModDependent("test_mod")]
            public class CondService : ICondService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.CondService");
        await Assert.That(type).IsNotNull();

        var factory = new FactoryModel(
            BaseType: "ICondService",
            ImplementationTypeName: "CondService",
            ImplementationNamespace: "DiPipelineTest",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Service(),
            OptionalModIds: ImmutableValueArray.From("test_mod"));

        var result = DiPipeline.ToRegistration(factory, type!);

        await Assert.That(result.ConditionalModIds).HasCount().EqualTo(1);
        await Assert.That(result.ConditionalModIds[0]).IsEqualTo("test_mod");
    }

    [Test]
    public async Task ToRegistration_NoConditionalModIds_EmptyArray(CancellationToken token)
    {
        TestSources.Add(("TestTypes.cs",
            """
            namespace DiPipelineTest;

            public interface INoCondService { }
            public class NoCondService : INoCondService { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiPipelineTest.NoCondService");
        await Assert.That(type).IsNotNull();

        var factory = new FactoryModel(
            BaseType: "INoCondService",
            ImplementationTypeName: "NoCondService",
            ImplementationNamespace: "DiPipelineTest",
            ConstructorArguments: [],
            RequiredProperties: [],
            Intent: new FactoryIntent.Service(),
            OptionalModIds: []);

        var result = DiPipeline.ToRegistration(factory, type!);

        await Assert.That(result.ConditionalModIds).HasCount().EqualTo(0);
    }

    // ── RenderConfigurator Tests (snapshot-verified) ────────────────────

    [Test]
    public async Task RenderConfigurator_ServiceKind_Complete_SingleUnconditional(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.MyService_Factory", []));

        var options = new ConfiguratorOptions(
            ClassName: "TestConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestConfigurator.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_ServiceKind_Complete_MultipleUnconditional(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.ServiceA_Factory", []),
            new RegistrationModel("global::TestNamespace.ServiceB_Factory", []),
            new RegistrationModel("global::TestNamespace.ServiceC_Factory", []));

        var options = new ConfiguratorOptions(
            ClassName: "MultiConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_ServiceKind_Partial_SingleUnconditional(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.MyService_Factory", []));

        var options = new ConfiguratorOptions(
            ClassName: "PartialConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: true);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_KeyedKind_Complete_SingleUnconditional(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.TestRegistry_KeyedFactory", []));

        var options = new ConfiguratorOptions(
            ClassName: "KeyedConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.Modding.IRegistryConfigurator",
            EntrypointAttribute: "Sparkitect.Modding.RegistryConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Keyed("Sparkitect.Modding.IRegistryBase"),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_ServiceKind_SingleConditional(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.OptService_Factory",
                ImmutableValueArray.From("color_mod")));

        var options = new ConfiguratorOptions(
            ClassName: "ConditionalConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_ServiceKind_MultipleConditionalModIds(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.MultiCondService_Factory",
                ImmutableValueArray.From("mod_a", "mod_b")));

        var options = new ConfiguratorOptions(
            ClassName: "MultiCondConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_ServiceKind_MixedRegistrations(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.ServiceA_Factory", []),
            new RegistrationModel("global::TestNamespace.ServiceB_Factory", []),
            new RegistrationModel("global::TestNamespace.OptService_Factory",
                ImmutableValueArray.From("color_mod")));

        var options = new ConfiguratorOptions(
            ClassName: "MixedConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_Partial_WithCustomMethodName(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.MyService_Factory", []));

        var options = new ConfiguratorOptions(
            ClassName: "CustomMethodConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: true,
            MethodName: "ConfigureRegistries");

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_WithModuleTypeFullName(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.MyService_Factory", []));

        var options = new ConfiguratorOptions(
            ClassName: "ModuleTypeConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false,
            ModuleTypeFullName: "global::TestNamespace.CoreModule");

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_KeyedKind_Partial_ConditionalRegistration(CancellationToken token)
    {
        var registrations = ImmutableValueArray.From(
            new RegistrationModel("global::TestNamespace.OptRegistry_KeyedFactory",
                ImmutableValueArray.From("ext_mod")));

        var options = new ConfiguratorOptions(
            ClassName: "KeyedPartialCondConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.Modding.IRegistryConfigurator",
            EntrypointAttribute: "Sparkitect.Modding.RegistryConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Keyed("Sparkitect.Modding.IRegistryBase"),
            IsPartial: true);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderConfigurator_EmptyRegistrations_StillRenders(CancellationToken token)
    {
        ImmutableValueArray<RegistrationModel> registrations = [];

        var options = new ConfiguratorOptions(
            ClassName: "EmptyConfigurator",
            Namespace: "TestNamespace",
            BaseType: "Sparkitect.GameState.IStateModuleServiceConfigurator",
            EntrypointAttribute: "Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute",
            Kind: new ConfiguratorKind.Service(),
            IsPartial: false);

        var success = DiPipeline.RenderConfigurator(registrations, options, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Verifier.Verify(code, verifySettings);
    }
}
