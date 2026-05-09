using System.Threading.Tasks;
using Sparkitect.Generator.Modding.Analyzers;

namespace Sparkitect.Generator.Tests.Modding.Analyzers;

public sealed class TypedRegistrationContractAnalyzerTests : AnalyzerTestBase<TypedRegistrationContractAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.ModdingCode);
        TestSources.Add(TestData.Sparkitect);
    }

    [Test]
    public async Task TypedRegistrationContract_PresentOnContract_NoDiagnostic()
    {
        // Positive case: TBase carries [TypedRegistrationContract] -> no diagnostic.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [TypedRegistrationContract]
        public interface IMyContract { }

        public partial class MyRegistry
        {
            [RegistryMethod]
            public void RegisterThing<T>(Identification id) where T : class, IMyContract, IHasIdentification
            {
            }
        }
        """;

        TestSources.Add(("MyRegistry.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task TypedRegistrationContract_MissingOnContract_ReportsSPARK0263()
    {
        // Negative case: TBase is user-source, not IHasIdentification, lacks the contract attribute.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IMyContract { }

        public partial class MyRegistry
        {
            [RegistryMethod]
            public void RegisterThing<T>(Identification id) where T : class, IMyContract, IHasIdentification
            {
            }
        }
        """;

        TestSources.Add(("MyRegistry.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0263", 1);
    }

    [Test]
    public async Task TypedRegistrationContract_BaseInterfaceCarriesAttribute_NoDiagnostic()
    {
        // Inherited=true: a contract interface that derives from a [TypedRegistrationContract]
        // base interface counts as carrying the attribute via the AllInterfaces walk.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        [TypedRegistrationContract]
        public interface IBaseContract { }

        public interface IDerivedContract : IBaseContract { }

        public partial class MyRegistry
        {
            [RegistryMethod]
            public void RegisterThing<T>(Identification id) where T : class, IDerivedContract, IHasIdentification
            {
            }
        }
        """;

        TestSources.Add(("MyRegistry.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task TypedRegistrationContract_NoTBaseConstraint_NoDiagnostic()
    {
        // No TBase candidate (only `class, IHasIdentification`) — analyzer skips.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public partial class MyRegistry
        {
            [RegistryMethod]
            public void RegisterThing<T>(Identification id) where T : class, IHasIdentification
            {
            }
        }
        """;

        TestSources.Add(("MyRegistry.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task TypedRegistrationContract_NotRegistryMethod_NoDiagnostic()
    {
        // Method without [RegistryMethod] is ignored even with the typed-registration shape.
        var code = """
        using Sparkitect.Modding;

        namespace DiTest;

        public interface IMyContract { }

        public partial class MyRegistry
        {
            public void RegisterThing<T>(Identification id) where T : class, IMyContract, IHasIdentification
            {
            }
        }
        """;

        TestSources.Add(("MyRegistry.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }
}
