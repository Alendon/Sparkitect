using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.Modding;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

/// <summary>
/// Unit tests for <see cref="IdentificationContract"/> — the cross-generator identification
/// predicate that recognises both direct <c>: IHasIdentification</c> declarations and
/// <c>[TypedRegistrationContract]</c>-marked base/interface contracts.
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
    public async Task DirectIHasIdentification_IsIdentified_True_HasContract_False(CancellationToken token)
    {
        TestSources.Add(("Direct.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            public class Direct : IHasIdentification { }
            """));

        var sym = await GetTypeAsync("IdContractTest.Direct", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsFalse();
    }

    [Test]
    public async Task ContractInterface_IsIdentified_True_HasContract_True(CancellationToken token)
    {
        TestSources.Add(("ContractIface.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            [TypedRegistrationContract]
            public interface IFoo { }

            public class FooImpl : IFoo { }
            """));

        var sym = await GetTypeAsync("IdContractTest.FooImpl", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsTrue();
    }

    [Test]
    public async Task ContractBaseClass_IsIdentified_True_HasContract_True(CancellationToken token)
    {
        TestSources.Add(("ContractBase.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            [TypedRegistrationContract]
            public abstract class FooBase { }

            public class FooDerived : FooBase { }
            """));

        var sym = await GetTypeAsync("IdContractTest.FooDerived", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsTrue();
    }

    [Test]
    public async Task SelfCarriesContract_IsIdentified_True_HasContract_True(CancellationToken token)
    {
        TestSources.Add(("SelfContract.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            [TypedRegistrationContract]
            public interface IBar { }
            """));

        var sym = await GetTypeAsync("IdContractTest.IBar", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsTrue();
    }

    [Test]
    public async Task DeeplyNested_IsIdentified_True_HasContract_True(CancellationToken token)
    {
        TestSources.Add(("Nested.cs",
            """
            using Sparkitect.Modding;
            namespace IdContractTest;

            [TypedRegistrationContract]
            public interface IRoot { }

            public class Mid : IRoot { }
            public class Leaf : Mid { }
            """));

        var sym = await GetTypeAsync("IdContractTest.Leaf", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsTrue();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsTrue();
    }

    [Test]
    public async Task PlainClass_IsIdentified_False_HasContract_False(CancellationToken token)
    {
        TestSources.Add(("Plain.cs",
            """
            namespace IdContractTest;

            public class Plain { }
            """));

        var sym = await GetTypeAsync("IdContractTest.Plain", token);

        await Assert.That(IdentificationContract.IsIdentified(sym)).IsFalse();
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsFalse();
    }

    [Test]
    public async Task UnrelatedAttributeOnly_IsIdentified_False_HasContract_False(CancellationToken token)
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
        await Assert.That(IdentificationContract.HasTypedRegistrationContract(sym)).IsFalse();
    }
}
