using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator;
using Sparkitect.Generator.Modding;
using static Sparkitect.Generator.Tests.TestData;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorAttributeArgParsingTests : SourceGeneratorTestBase<RegistryGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        TestSources.Add(GlobalUsings);
        TestSources.Add(DiAttributes);
        TestSources.Add(ModdingCode);
    }

    [Test]
    public async Task TryParseProviderArguments_Method_WithFiles()
    {
        TestSources.Add(("ArgParse.Method.cs", """
        using Sparkitect.DI.GeneratorAttributes;
        using Sparkitect.Modding;

        namespace DiTest
        {
            [Registry(Identifier = "dummy")]
            public class DummyRegistry : IRegistry
            {
                [RegistryMethod] public void RegisterValue(Identification id, string value) { }
            }

            public static class Providers
            {
                [DummyRegistry.RegisterValue("hello", File = "a.txt", Foo = "b.txt")]
                public static string Value() => "x";
            }
        }
        """));

        var (_, compilation) = await GetInitialCompilationAsync(CancellationToken.None);
        var providers = compilation.GetTypeByMetadataName("DiTest.Providers")!;
        var method = providers.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "Value");
        var attr = method.GetAttributes().First();
        var node = (AttributeSyntax)attr.ApplicationSyntaxReference!.GetSyntax(CancellationToken.None);

        var ok = RegistryGenerator.TryParseProviderArguments(node, out var id, out var files);
        await Assert.That(ok).IsTrue();
        await Assert.That(id).IsEqualTo("hello");
        await Assert.That(files.Count).IsEqualTo(2);
        await Assert.That(files.Any(f => f.PropertyName == "File" && f.FileName == "a.txt")).IsTrue();
        await Assert.That(files.Any(f => f.PropertyName == "Foo" && f.FileName == "b.txt")).IsTrue();
    }

    [Test]
    public async Task TryParseProviderArguments_Property_IdOnly()
    {
        TestSources.Add(("ArgParse.Property.cs", """
        using Sparkitect.DI.GeneratorAttributes;
        using Sparkitect.Modding;

        namespace DiTest
        {
            [Registry(Identifier = "dummy")]
            public class DummyRegistry : IRegistry
            {
                [RegistryMethod] public void RegisterValue(Identification id, string value) { }
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

        var ok = RegistryGenerator.TryParseProviderArguments(node, out var id, out var files);
        await Assert.That(ok).IsTrue();
        await Assert.That(id).IsEqualTo("pid");
        await Assert.That(files.Count).IsEqualTo(0);
    }
}

