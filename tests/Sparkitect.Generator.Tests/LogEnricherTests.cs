using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Model;
using Sparkitect.Generator.LogEnricher;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests;

public class LogEnricherTests : TestBase<LogEnricherGenerator>
{
    private static readonly ReferenceAssemblies SerilogReferences = ReferenceAssemblies.Net.Net90
        .WithPackages([new PackageIdentity("Serilog", "4.2.0")]);

    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("Serilog", "4.2.0")]);
    }


    [Test]
    public async Task SingleLogStatement(CancellationToken token)
    {
        var testSource = """
                         using Serilog;

                         namespace TestNamespace
                         {
                             public class TestClass
                             {
                                 public void TestMethod()
                                 {
                                     Log.Information("Test {Value}", 42);
                                 }
                             }
                         }

                         """;
        

        TestState.Sources.Add(("TestClass.cs", testSource));
        TestState.AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = ValidationTest
            build_property.DisableLogEnrichmentGenerator = false
            """));
        TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck |
                        TestBehaviors.SkipSuppressionCheck;

        await RunAsync(token);


        Verifier.Verify(TestState.GeneratedSources);
    }
}