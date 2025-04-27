using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.LogEnricher;

namespace Sparkitect.Generator.Tests;

public class TestBase<TSourceGenerator> : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    [Before(Test)]
    public void SetupBase()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
    }
    
    
    
}