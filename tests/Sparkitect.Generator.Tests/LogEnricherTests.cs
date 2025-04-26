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
    public async Task SingleLogStatement()
    {
        CSharpSourceGeneratorTest<LogEnricherGenerator, DefaultVerifier> a = new()
        {
            ReferenceAssemblies = SerilogReferences,
            TestState =
            {
                AnalyzerConfigFiles = { }
            }
        };

        await a.RunAsync();
        a.CreateWorkspaceAsync();
        
        var testState = TestState;


        var project = await CreateProjectAsync(new EvaluatedProjectState(testState, ReferenceAssemblies),
            [
                ..testState.AdditionalProjects.Values
                    .Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies))
            ], CancellationToken.None);
        var compilation = await project.GetCompilationAsync();


        CSharpSourceGeneratorVerifier<LogEnricherGenerator, DefaultVerifier> b = new();
    }
}