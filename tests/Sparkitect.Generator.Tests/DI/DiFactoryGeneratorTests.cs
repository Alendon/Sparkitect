using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.DI;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests.DI;

public class DiFactoryGeneratorTests : SourceGeneratorTestBase<DiFactoryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.SparkitectCore);
    }

    public override ModBuildSettings BuildSettings { get; }

    [Test]
    public async Task DiGenerator_FullRun_NoDependencies(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface ITestService {}

            [Singleton<ITestService>]
            public class TestService : ITestService {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractSingletonModelData_NoDependencies(CancellationToken token)
    {
        TestSources.Add(("TestService.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface ITestService {}

            [Singleton<ITestService>]
            public class TestService : ITestService {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.TestService");

        await Assert.That(type).IsNotNull();

        var model = DiFactoryGenerator.ExtractServiceFactoryModelData(type!);


        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ConstructorArguments).IsEmpty();
        await Assert.That(model.RequiredProperties).IsEmpty();
        await Assert.That(model.ImplementationNamespace).IsEqualTo("DiTest");
        await Assert.That(model.ServiceType).IsEqualTo("global::DiTest.ITestService");
        await Assert.That(model.ImplementationTypeName).IsEqualTo("TestService");
    }

    [Test]
    public async Task RenderSingletonFactory(CancellationToken token)
    {
        var model = new ServiceFactoryModel(
            "global::DiTest.ITestService",
            "TestService",
            "DiTest",
            ImmutableValueArray.From(
                new ConstructorArgument("global::DiTest.IDependencyA", false),
                new ConstructorArgument("global::DiTest.IDependencyB", true)
            ),
            ImmutableValueArray.From(
                new RequiredProperty("global::DiTest.IDependencyC", "set_C", false),
                new RequiredProperty("global::DiTest.IDependencyD", "set_D", true)
            )
        );

        var success = DiFactoryGenerator.RenderServiceFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestService_Factory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task DiGenerator_EntrypointFactory_FullRun(CancellationToken token)
    {
        TestSources.Add(("TestEntrypoint.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.DI;
            namespace DiTest;

            public interface IMyEntrypoint {}

            [EntrypointAttribute<IMyEntrypoint>]
            public class TestEntrypoint : ConfigurationEntrypoint<EntrypointAttribute<IMyEntrypoint>>, IMyEntrypoint 
            {
                public TestEntrypoint(IService1 service1, IService2? service2) {}
                
                public required IService3 Service3 { get; init; }
            }

            public interface IService1 {}
            public interface IService2 {}
            public interface IService3 {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractEntrypointFactoryModelData(CancellationToken token)
    {
        TestSources.Add(("TestEntrypoint.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface IMyEntrypoint {}

            [EntrypointFactoryAttribute<IMyEntrypoint>]
            public class TestEntrypoint : IMyEntrypoint 
            {
                public TestEntrypoint() {}
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.TestEntrypoint");

        await Assert.That(type).IsNotNull();

        var model = DiFactoryGenerator.ExtractEntrypointFactoryModelData(type!);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ConstructorArguments).IsEmpty();
        await Assert.That(model.RequiredProperties).IsEmpty();
        await Assert.That(model.ImplementationNamespace).IsEqualTo("DiTest");
        await Assert.That(model.BaseType).IsEqualTo("global::DiTest.IMyEntrypoint");
        await Assert.That(model.ImplementationTypeName).IsEqualTo("TestEntrypoint");
    }

    [Test]
    public async Task RenderEntrypointFactory(CancellationToken token)
    {
        var model = new EntrypointFactoryModel(
            "global::DiTest.IMyEntrypoint",
            "TestEntrypoint",
            "DiTest",
            ImmutableValueArray.From(
                new ConstructorArgument("global::DiTest.IService1", false),
                new ConstructorArgument("global::DiTest.IService2", true)
            ),
            ImmutableValueArray.From(
                new RequiredProperty("global::DiTest.IService3", "set_Service3", false)
            )
        );

        var success = DiFactoryGenerator.RenderEntrypointFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("TestEntrypoint_EntrypointFactory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task DiGenerator_KeyedFactory_DirectKey_FullRun(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(Key = "json")]
            public class JsonProcessor : IProcessor 
            {
                public JsonProcessor(ILogger logger) {}
            }

            public interface ILogger {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);

        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task DiGenerator_KeyedFactory_PropertyKey_FullRun(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            using OneOf;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(KeyPropertyName = nameof(ProcessorKey)]
            public class XmlProcessor : IProcessor 
            {
                public static OneOf<Identification, string> ProcessorKey => new Identification("xml_processor");
                
                public XmlProcessor(ILogger logger) {}
                
                public required ISerializer Serializer { get; init; }
            }

            public interface ILogger {}
            public interface ISerializer {}
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);
        await Verifier.Verify(driverRunResult, verifySettings);
    }

    [Test]
    public async Task ExtractKeyedFactoryModelData_DirectKey(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(Key = "json")]
            public class JsonProcessor : IProcessor 
            {
                public JsonProcessor() {}
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.JsonProcessor");

        await Assert.That(type).IsNotNull();

        var model = DiFactoryGenerator.ExtractKeyedFactoryModelData(type!);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.ConstructorArguments).IsEmpty();
        await Assert.That(model.RequiredProperties).IsEmpty();
        await Assert.That(model.ImplementationNamespace).IsEqualTo("DiTest");
        await Assert.That(model.BaseType).IsEqualTo("global::DiTest.IProcessor");
        await Assert.That(model.ImplementationTypeName).IsEqualTo("JsonProcessor");
        await Assert.That(model.KeyInfo).IsNotNull();
        await Assert.That(model.KeyInfo).IsTypeOf<DirectKeyInfo>();
        await Assert.That(((DirectKeyInfo)model.KeyInfo).KeyValue).IsEqualTo("json");
    }

    [Test]
    public async Task RenderKeyedFactory_DirectKey(CancellationToken token)
    {
        var model = new KeyedFactoryModel(
            "global::DiTest.IProcessor",
            "JsonProcessor",
            "DiTest",
            ImmutableValueArray.From(
                new ConstructorArgument("global::DiTest.ILogger", false)
            ),
            ImmutableValueArray.From(
                new RequiredProperty("global::DiTest.ISerializer", "set_Serializer", true)
            ),
            new DirectKeyInfo("json")
        );

        var success = DiFactoryGenerator.RenderKeyedFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("JsonProcessor_KeyedFactory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task RenderKeyedFactory_PropertyKey(CancellationToken token)
    {
        var model = new KeyedFactoryModel(
            "global::DiTest.IProcessor",
            "XmlProcessor",
            "DiTest",
            ImmutableValueArray.From(
                new ConstructorArgument("global::DiTest.ILogger", false)
            ),
            [],
            new PropertyKeyInfo("ProcessorKey", "OneOf<Identification, string>")
        );

        var success = DiFactoryGenerator.RenderKeyedFactory(model, out var code, out var fileName);

        await Assert.That(success).IsTrue();
        await Assert.That(fileName).IsEqualTo("XmlProcessor_KeyedFactory.g.cs");
        await Verifier.Verify(code, verifySettings);
    }

    [Test]
    public async Task ExtractKeyedFactoryModelData_PropertyKey(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            using OneOf;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(KeyPropertyName = nameof(ProcessorKey))]
            public class XmlProcessor : IProcessor 
            {
                public static OneOf<Identification, string> ProcessorKey => "xml";
                public XmlProcessor() {}
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.XmlProcessor");

        await Assert.That(type).IsNotNull();

        var model = DiFactoryGenerator.ExtractKeyedFactoryModelData(type!);

        await Assert.That(model).IsNotNull();
        await Assert.That(model!.KeyInfo).IsNotNull();
        await Assert.That(model.KeyInfo).IsTypeOf<PropertyKeyInfo>();

        var propertyKeyInfo = (PropertyKeyInfo)model.KeyInfo!;
        await Assert.That(propertyKeyInfo.PropertyName).IsEqualTo("ProcessorKey");
        await Assert.That(propertyKeyInfo.ReturnType)
            .IsEqualTo("global::OneOf.OneOf<global::Sparkitect.Modding.Identification, string>");
    }

    [Test]
    public async Task ExtractKeyInfo_DirectKey(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(Key = "json")]
            public class JsonProcessor : IProcessor {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.JsonProcessor");
        await Assert.That(type).IsNotNull();

        var factoryAttribute = type!.GetAttributes().FirstOrDefault(x => DiUtils.FindFactoryMarker(x) is not null);
        await Assert.That(factoryAttribute).IsNotNull();

        var keyInfo = DiFactoryGenerator.ExtractKeyInfo(factoryAttribute!, type);

        await Assert.That(keyInfo).IsNotNull();
        await Assert.That(keyInfo).IsTypeOf<DirectKeyInfo>();
        await Assert.That(((DirectKeyInfo)keyInfo!).KeyValue).IsEqualTo("json");
    }

    [Test]
    public async Task ExtractKeyInfo_PropertyKey(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>(KeyPropertyName = nameof(Key))]
            public class JsonProcessor : IProcessor {
                public static string Key => "json";
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.JsonProcessor");
        await Assert.That(type).IsNotNull();

        var factoryAttribute = type!.GetAttributes().FirstOrDefault(x => DiUtils.FindFactoryMarker(x) is not null);
        await Assert.That(factoryAttribute).IsNotNull();

        var keyInfo = DiFactoryGenerator.ExtractKeyInfo(factoryAttribute!, type);

        await Assert.That(keyInfo).IsNotNull();
        await Assert.That(keyInfo).IsTypeOf<PropertyKeyInfo>();
        await Assert.That(((PropertyKeyInfo)keyInfo!).PropertyName).IsEqualTo("Key");
        await Assert.That(((PropertyKeyInfo)keyInfo).ReturnType).IsEqualTo("string");
    }

    [Test]
    public async Task ExtractKeyInfo_NoKey_ReturnsNull(CancellationToken token)
    {
        TestSources.Add(("TestKeyedFactory.cs",
            """
            using Sparkitect.DI.GeneratorAttributes;
            namespace DiTest;

            public interface IProcessor {}

            [KeyedFactory<IProcessor>()]  // No key provided
            public class JsonProcessor : IProcessor {}
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);
        var type = compilation.GetTypeByMetadataName("DiTest.JsonProcessor");
        await Assert.That(type).IsNotNull();

        var factoryAttribute = type!.GetAttributes().FirstOrDefault(x => DiUtils.FindFactoryMarker(x) is not null);
        await Assert.That(factoryAttribute).IsNotNull();

        var keyInfo = DiFactoryGenerator.ExtractKeyInfo(factoryAttribute!, type);

        await Assert.That(keyInfo).IsNull();
    }
}