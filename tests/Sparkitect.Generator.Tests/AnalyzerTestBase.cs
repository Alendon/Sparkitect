using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyTests;

namespace Sparkitect.Generator.Tests;

public abstract class AnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected readonly VerifySettings verifySettings = new();

    [Before(Test)]
    public void SetupBase()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        verifySettings.UseDirectory("TestResults");
    }

    /// <summary>
    /// Gets the language name used for the test.
    /// </summary>
    protected virtual string LanguageName => LanguageNames.CSharp;

    /// <summary>
    /// Gets or sets the source files to include in the test compilation.
    /// Tuple is (Filename, SourceText or string content).
    /// </summary>
    public List<(string Filename, object Content)> TestSources { get; } = new();

    /// <summary>
    /// Gets or sets the reference assemblies to use for the compilation.
    /// </summary>
    public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net90;

    /// <summary>
    /// Gets or sets the analyzer config files (.editorconfig) to include.
    /// </summary>
    public List<(string Path, object Content)> AnalyzerConfigFiles { get; } = new();

    /// <summary>
    /// Gets or sets the target C# language version for the compilation.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Latest;

    /// <summary>
    /// Gets or sets the OutputKind for the compilation.
    /// </summary>
    public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;

    /// <summary>
    /// Gets or sets the NullableContextOptions for the compilation.
    /// </summary>
    public NullableContextOptions NullableContextOptions { get; set; } = NullableContextOptions.Enable;

    /// <summary>
    /// Gets or sets whether to allow unsafe code.
    /// </summary>
    public bool AllowUnsafe { get; set; } = true;

    /// <summary>
    /// Additional metadata references to add to the compilation
    /// </summary>
    public List<MetadataReference> AdditionalReferences { get; } = new();

    /// <summary>
    /// Runs the analyzer on the test sources and returns the diagnostics.
    /// </summary>
    public async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(CancellationToken cancellationToken = default)
    {
        var compilation = await CreateCompilationAsync(cancellationToken);
        var analyzer = new TAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            new CompilationWithAnalyzersOptions(null, null, true, false));

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken);

        // Filter out non-analyzer diagnostics
        var analyzerDiagnosticIds = analyzer.SupportedDiagnostics.Select(d => d.Id).ToImmutableHashSet();
        return diagnostics.Where(d => analyzerDiagnosticIds.Contains(d.Id)).ToImmutableArray();
    }

    /// <summary>
    /// Creates a compilation from the test sources.
    /// </summary>
    protected async Task<CSharpCompilation> CreateCompilationAsync(CancellationToken cancellationToken = default)
    {
        var syntaxTrees = new List<SyntaxTree>();

        foreach (var (filename, content) in TestSources)
        {
            var sourceText = GetSourceText(content, filename);
            var tree = CSharpSyntaxTree.ParseText(sourceText,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion), filename,
                cancellationToken: cancellationToken);
            syntaxTrees.Add(tree);
        }

        var references = await ReferenceAssemblies.ResolveAsync(LanguageName, cancellationToken);
        references = references.AddRange(AdditionalReferences);

        var compilationOptions = new CSharpCompilationOptions(
                OutputKind,
                allowUnsafe: AllowUnsafe,
                nullableContextOptions: NullableContextOptions,
                specificDiagnosticOptions: GetNullableWarningsFromCompiler())
            .WithUsings(
                "global::System;",
                "global::System.Collections.Generic;",
                "global::System.IO;",
                "global::System.Linq;",
                "global::System.Net.Http;",
                "global::System.Threading;",
                "global::System.Threading.Tasks;"
            );

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            compilationOptions);
    }

    /// <summary>
    /// Helper method to verify that a diagnostic is reported at the expected location.
    /// </summary>
    protected static async Task AssertDiagnostic(ImmutableArray<Diagnostic> diagnostics, string diagnosticId, int line,
        int column, params object[] messageArgs)
    {
        var diagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
        await Assert.That(diagnostic).IsNotNull()
            .Because($"Expected diagnostic '{diagnosticId}' was not found");

        var location = diagnostic!.Location;
        var lineSpan = location.GetLineSpan();

        await Assert.That(lineSpan.StartLinePosition.Line).IsEqualTo(line - 1)
            .Because($"Expected diagnostic at line {line}, but was at line {lineSpan.StartLinePosition.Line + 1}");
        await Assert.That(lineSpan.StartLinePosition.Character).IsEqualTo(column - 1)
            .Because(
                $"Expected diagnostic at column {column}, but was at column {lineSpan.StartLinePosition.Character + 1}");

        // Verify message arguments if provided
        if (messageArgs.Length > 0)
        {
            var message = diagnostic.GetMessage();
            foreach (var arg in messageArgs)
            {
                await Assert.That(message).Contains(arg.ToString()!);
            }
        }
    }

    /// <summary>
    /// Helper method to verify that no diagnostics are reported.
    /// </summary>
    protected static async Task AssertNoDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.Any())
        {
            var messages = string.Join("\n", diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
            Assert.Fail($"Expected no diagnostics, but found:\n{messages}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to verify specific diagnostics count.
    /// </summary>
    protected static async Task AssertDiagnosticCount(ImmutableArray<Diagnostic> diagnostics, string diagnosticId,
        int expectedCount)
    {
        var count = diagnostics.Count(d => d.Id == diagnosticId);
        await Assert.That(count).IsEqualTo(expectedCount)
            .Because($"Expected {expectedCount} occurrences of diagnostic '{diagnosticId}', but found {count}");
    }

    private SourceText GetSourceText(object content, string filePath) => content switch
    {
        string text => SourceText.From(text, System.Text.Encoding.UTF8),
        SourceText sourceText => sourceText,
        _ => throw new ArgumentException(
            $"Unsupported source content type: {content.GetType()} for file {filePath}", nameof(content)),
    };

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        var args = new[] { "/warnaserror:nullable" };
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(args,
            baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
        return commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;
    }
}