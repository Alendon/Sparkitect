using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

/// <summary>
/// Unit tests for <see cref="IdentificationContract"/> — the cross-generator identification
/// predicate. It recognises a type as identified when <c>IHasIdentification</c> is present in its
/// <see cref="INamedTypeSymbol.AllInterfaces"/>, whether declared directly or inherited.
/// </summary>
public class IdentificationContractTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);
    }

    private async Task<INamedTypeSymbol> GetTypeAsync(string fullyQualifiedMetadataName, CancellationToken ct)
    {
        var (_, compilation) = await GetInitialCompilationAsync(ct);
        var sym = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        await Assert.That(sym).IsNotNull();
        return sym!;
    }

    [Test]
    public async Task DirectIHasIdentification_IsIdentified_True(CancellationToken token)
    {
        TestSources.Add(("Direct.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            public class Direct : IHasIdentification { }
            """));

        var sym = await GetTypeAsync("IdContractTest.Direct", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
    }

    [Test]
    public async Task InheritedViaInterfaceChain_IsIdentified_True(CancellationToken token)
    {
        TestSources.Add(("Iface.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            public interface IFoo : IHasIdentification { }

            public class FooImpl : IFoo { }
            """));

        var sym = await GetTypeAsync("IdContractTest.FooImpl", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
    }

    [Test]
    public async Task InheritedViaBaseClass_IsIdentified_True(CancellationToken token)
    {
        TestSources.Add(("Base.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            public abstract class FooBase : IHasIdentification { }

            public class FooDerived : FooBase { }
            """));

        var sym = await GetTypeAsync("IdContractTest.FooDerived", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
    }

    [Test]
    public async Task PlainClass_IsIdentified_False(CancellationToken token)
    {
        TestSources.Add(("Plain.cs",
            """
            namespace IdContractTest;

            public class Plain { }
            """));

        var sym = await GetTypeAsync("IdContractTest.Plain", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsFalse();
    }

    [Test]
    public async Task UnrelatedAttributeOnly_IsIdentified_False(CancellationToken token)
    {
        TestSources.Add(("Unrelated.cs",
            """
            namespace IdContractTest;

            public class UnrelatedAttribute : System.Attribute { }

            [Unrelated]
            public class WithUnrelated { }
            """));

        var sym = await GetTypeAsync("IdContractTest.WithUnrelated", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsFalse();
    }
}
