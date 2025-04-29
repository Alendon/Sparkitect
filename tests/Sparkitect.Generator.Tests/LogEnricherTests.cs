using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.LogEnricher;
using VerifyTUnit;

namespace Sparkitect.Generator.Tests;

public class LogEnricherTests : SourceGeneratorTestBase<LogEnricherGenerator>
{

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

                         namespace ValidationTest
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
        
        TestSources.Add(("TestClass.cs", testSource));
        
        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = ValidationTestMod
            build_property.DisableLogEnrichmentGenerator = false
            build_property.RootNamespace = ValidationTest
            """));

        var (_, driverRunResult) = await RunGeneratorAsync(token);


        await Verifier.Verify(driverRunResult, verifySettings);
    }
}