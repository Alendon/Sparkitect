using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator;
using Sparkitect.Generator.Modding;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorProviderCandidateTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);
    }
    
    [Test]
    public async Task TryBuildProviderCandidate_Method_Happy()
    {
        TestSources.Add(("P.Method.cs", """
        using Sparkitect.DI.GeneratorAttributes;
        using Sparkitect.Modding;

        namespace DiTest
        {
            public interface IDep { }
            public interface IOther { }

            [Registry(Identifier = "dummy")]
            public class DummyRegistry : IRegistry
            {
                [RegistryMethod]
                public void RegisterValue(Identification id, string value) { }
            }

            public static class Providers
            {
                [DummyRegistry.RegisterValue("hello", File = "foo.png")]
                public static string Value(IDep dep, IOther? other) => "x";
            }
        }
        """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        var providers = compilation.GetTypeByMetadataName("DiTest.Providers")!;
        var method = providers.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "Value");
        var attr = method.GetAttributes().First();
        var node = (AttributeSyntax)attr.ApplicationSyntaxReference!.GetSyntax(CancellationToken.None);

        var cand = RegistryGenerator.TryBuildProviderCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), CancellationToken.None);
        await Assert.That(cand).IsNotNull();
        await Assert.That(cand!.RegistryTypeName).IsEqualTo("DummyRegistry");
        await Assert.That(cand.MethodName).IsEqualTo("RegisterValue");
        await Assert.That(cand.Id).IsEqualTo("hello");
        await Assert.That(cand.IsTypeProvider).IsFalse();
        await Assert.That(cand.IsPropertyProvider).IsFalse();
        await Assert.That(cand.ProviderContainingTypeFullName).IsEqualTo("DiTest.Providers");
        await Assert.That(cand.ProviderMethodOrTypeName).IsEqualTo("Value");
        await Assert.That(cand.Files.Count).IsEqualTo(1);
        await Assert.That(cand.DiParameters.Count).IsEqualTo(2);
        await Assert.That(cand.DiParameters.First().paramType).Contains("DiTest.IDep");
        await Assert.That(cand.DiParameters.First().isNullable).IsFalse();
        await Assert.That(cand.DiParameters.Last().isNullable).IsTrue();
    }

    [Test]
    public async Task TryBuildProviderCandidate_Property_Happy()
    {
        TestSources.Add(("P.Property.cs", """
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
                [DummyRegistry.RegisterValue("pid")]
                public static string Value => "x";
            }
        }
        """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        var providers = compilation.GetTypeByMetadataName("DiTest.Providers")!;
        var prop = providers.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "Value");
        var attr = prop.GetAttributes().First();
        var node = (AttributeSyntax)attr.ApplicationSyntaxReference!.GetSyntax(CancellationToken.None);

        var cand = RegistryGenerator.TryBuildProviderCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), CancellationToken.None);
        await Assert.That(cand).IsNotNull();
        await Assert.That(cand!.IsPropertyProvider).IsTrue();
        await Assert.That(cand.ProviderMethodOrTypeName).IsEqualTo("Value");
        await Assert.That(cand.DiParameters.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryBuildProviderCandidate_Type_Happy()
    {
        TestSources.Add(("P.Type.cs", """
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

            [DummyRegistry.RegisterType("tid")]
            public class Provided {}
        }
        """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);

        var provided = compilation.GetTypeByMetadataName("DiTest.Provided")!;
        var attr = provided.GetAttributes().First();
        var node = (AttributeSyntax)attr.ApplicationSyntaxReference!.GetSyntax(CancellationToken.None);

        var cand = RegistryGenerator.TryBuildProviderCandidate(node, compilation.GetSemanticModel(node.SyntaxTree), CancellationToken.None);
        await Assert.That(cand).IsNotNull();
        await Assert.That(cand!.IsTypeProvider).IsTrue();
        await Assert.That(cand.ProviderMethodOrTypeName).Contains("DiTest.Provided");
        await Assert.That(cand.DiParameters.Count).IsEqualTo(0);
    }

}

