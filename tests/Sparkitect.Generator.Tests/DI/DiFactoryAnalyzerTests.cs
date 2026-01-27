using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.DI;

namespace Sparkitect.Generator.Tests.DI;

public class DiFactoryAnalyzerTests : AnalyzerTestBase<DiFactoryAnalyzer>
{

    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.Sparkitect);
    }

    [Test]
    public async Task SingleConstructor_WhenMultipleConstructors_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public Service() { }
                public Service(string name) { }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0102", 1);
    }

    [Test]
    public async Task SingleConstructor_WhenOneConstructor_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public Service() { }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task AbstractDependencies_WhenConcreteParameter_ReportsWarning()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public interface IDependency { }
            public class ConcreteDependency : IDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public Service(ConcreteDependency dependency) { }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0101", 1);
    }

    [Test]
    public async Task AbstractDependencies_WhenInterfaceParameter_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public interface IDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public Service(IDependency dependency) { }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task AbstractDependencies_WhenAbstractClassParameter_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public abstract class AbstractDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public Service(AbstractDependency dependency) { }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task RequiredProperties_WhenNotInitOnly_ReportsWarning()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public interface IDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public required IDependency Dependency { get; set; }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0103", 1);
        await AssertDiagnostic(diagnostics, "SPARK0103", 9, 33, "Dependency");
    }

    [Test]
    public async Task RequiredProperties_WhenInitOnly_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public interface IDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public required IDependency Dependency { get; init; }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task RequiredProperties_WhenConcreteType_ReportsWarning()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public class ConcreteDependency { }
            
            [Singleton<IService>]
            public class Service : IService
            {
                public required ConcreteDependency Dependency { get; init; }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0101", 1);
    }

    [Test]
    public async Task MultipleGenerationMarkers_WhenSameType_ReportsWarning()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [Singleton<IService>]
            [Singleton<IService>]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0104", 1);
    }

    [Test]
    public async Task ConflictingGenerationMarkers_WhenDifferentTypes_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [Singleton<IService>]
            [KeyedFactory<IService>(key: "key")]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0105", 1);
    }

    [Test]
    public async Task KeyedFactory_WhenNoKey_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0106", 1);
    }

    [Test]
    public async Task KeyedFactory_WithDirectKey_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(Key = "myKey")]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task KeyedFactory_WithValidKeyProperty_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static string ServiceKey => "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task KeyedFactory_WithInvalidKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(NonExistentKey))]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0107", 1);
    }

    [Test]
    public async Task KeyedFactory_WithNonStaticKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public string ServiceKey => "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0107", 1);
    }

    [Test]
    public async Task KeyedFactory_WithBothKeyAndKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(Key = "directKey", KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static string ServiceKey => "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0108", 1);
    }

    [Test]
    public async Task ComplexScenario_MultipleIssues_ReportsAll()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            public interface IDependency { }
            public class ConcreteDependency : IDependency { }
            
            [Singleton<IService>]
            [Singleton<IService>]
            public class Service : IService
            {
                public Service(ConcreteDependency dependency) { }
                public Service() { }
                
                public required ConcreteDependency RequiredDep { get; set; }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        // Should have multiple diagnostics
        await Assert.That(diagnostics.Length).IsGreaterThanOrEqualTo(4);
        await AssertDiagnosticCount(diagnostics, "SPARK0101", 2); // Two concrete dependencies
        await AssertDiagnosticCount(diagnostics, "SPARK0102", 1); // Multiple constructors
        await AssertDiagnosticCount(diagnostics, "SPARK0103", 1); // Required property not init-only
        await AssertDiagnosticCount(diagnostics, "SPARK0104", 1); // Multiple same markers
    }

    [Test]
    public async Task KeyedFactory_WithPrivateKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                private static string ServiceKey => "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0107", 1);
    }

    [Test]
    public async Task KeyedFactory_WithWriteOnlyKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static string ServiceKey { set { } }
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0107", 1);
    }

    [Test]
    public async Task KeyedFactory_WithIdentificationKeyProperty_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static Identification ServiceKey => new("myKey");
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task KeyedFactory_WithOneOfKeyProperty_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            using Sparkitect.Modding;
            using OneOf;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static OneOf<Identification, string> ServiceKey => "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task KeyedFactory_WithInvalidReturnTypeKeyProperty_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static int ServiceKey => 42;
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0107", 1);
    }

    [Test]
    public async Task KeyedFactory_WithEmptyKey_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(Key = "")]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0106", 1);
    }

    [Test]
    public async Task KeyedFactory_WithEmptyKeyPropertyName_ReportsError()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = "")]
            public class Service : IService { }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertDiagnosticCount(diagnostics, "SPARK0106", 1);
    }

    [Test]
    public async Task KeyedFactory_WithPublicGetterPrivateSetter_NoDiagnostic()
    {
        var code = """
            using Sparkitect.DI.GeneratorAttributes;
            
            public interface IService { }
            
            [KeyedFactory<IService>(KeyPropertyName = nameof(ServiceKey))]
            public class Service : IService 
            {
                public static string ServiceKey { get; private set; } = "myKey";
            }
            """;
        
        TestSources.Add(("Service.cs", code));
        
        var diagnostics = await RunAnalyzerAsync();
        
        await AssertNoDiagnostics(diagnostics);
    }
}