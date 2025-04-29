using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyTests;

namespace Sparkitect.Generator.Tests;

public abstract class SourceGeneratorTestBase<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    
    protected readonly VerifySettings verifySettings = new();
    
    
    [Before(Test)]
    public void SetupBase()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        verifySettings.UseDirectory("TestResults");
    }
    
    private static readonly Lazy<Workspace> Workspace = new(CreateWorkspace);

    /// <summary>
    /// Gets the language name used for the test.
    /// </summary>
    protected virtual string LanguageName => LanguageNames.CSharp;

    /// <summary>
    /// Gets the default file extension used for source files.
    /// </summary>
    protected virtual string DefaultFileExt => ".cs";

    /// <summary>
    /// Gets or sets the source files to include in the test compilation.
    /// Tuple is (Filename, SourceText or string content).
    /// </summary>
    public List<(string Filename, object Content)> TestSources { get; } = new();

    /// <summary>
    /// Gets or sets the reference assemblies to use for the compilation.
    /// Defaults to ReferenceAssemblies.Net.Net90, but can be overridden in derived classes or tests.
    /// </summary>
    public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net90;

    /// <summary>
    /// Gets or sets the analyzer config files (.editorconfig) to include.
    /// Tuple is (Path, SourceText or string content).
    /// </summary>
    public List<(string Path, object Content)> AnalyzerConfigFiles { get; } = new();

    /// <summary>
    /// Gets or sets the target C# language version for the compilation.
    /// Defaults to LanguageVersion.Latest.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Latest;

    /// <summary>
    /// Gets or sets the OutputKind for the compilation.
    /// Defaults to OutputKind.DynamicallyLinkedLibrary.
    /// </summary>
    public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;

    /// <summary>
    /// Gets or sets the Platform for the compilation.
    /// Defaults to Platform.AnyCpu.
    /// </summary>
    public Platform Platform { get; set; } = Platform.AnyCpu;

    /// <summary>
    /// Gets or sets the NullableContextOptions for the compilation.
    /// Defaults to NullableContextOptions.Enable.
    /// </summary>
    public NullableContextOptions NullableContextOptions { get; set; } = NullableContextOptions.Enable;

    /// <summary>
    /// Gets or sets whether to allow unsafe code.
    /// Defaults to true.
    /// </summary>
    public bool AllowUnsafe { get; set; } = true;

    /// <summary>
    /// Allows customization of compilation options in derived classes.
    /// </summary>
    protected virtual CSharpCompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(
            OutputKind,
            allowUnsafe: AllowUnsafe,
            platform: Platform,
            nullableContextOptions: NullableContextOptions);

    /// <summary>
    /// Allows customization of parse options in derived classes.
    /// </summary>
    protected virtual CSharpParseOptions CreateParseOptions()
        => new CSharpParseOptions(LanguageVersion, DocumentationMode.Diagnose);

    /// <summary>
    /// Creates and returns the initial Roslyn Compilation based on the current test setup
    /// (sources, references, options) *without* running the source generator.
    /// Useful for unit testing components that require a Compilation or SemanticModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initial Compilation and the Project it belongs to.</returns>
    /// <exception cref="InvalidOperationException">Thrown if project creation or compilation retrieval fails.</exception>
    public async Task<(Project Project, Compilation Compilation)> GetInitialCompilationAsync(
        CancellationToken cancellationToken = default)
    {
        var project = await CreateProjectAsync(cancellationToken);
        var compilation = await project.GetCompilationAsync(cancellationToken);

        if (compilation is null)
        {
            throw new InvalidOperationException("Failed to retrieve compilation from project.");
        }

        // Check for initial compilation errors (useful for debugging test setup)
        var diagnostics = compilation.GetDiagnostics(cancellationToken);
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            // Consider logging or handling these errors appropriately
            // For now, we proceed, as the generator might fix them or they might be expected
        }

        return (project, compilation);
    }

    /// <summary>
    /// Runs the source generator defined by TSourceGenerator on the test sources and returns the results.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the final Compilation (with generated code included) and the GeneratorDriverRunResult</returns>
    public async Task<(Compilation NewCompilation, GeneratorDriverRunResult driver)>
        RunGeneratorAsync(CancellationToken cancellationToken = default)
    {
        var (project, compilation) = await GetInitialCompilationAsync(cancellationToken);
        var parseOptions = CreateParseOptions();
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(AnalyzerConfigFiles);

        // Create an instance of the generator
        var generator = new TSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);

        // Run the generator
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation,
            out _, cancellationToken) as CSharpGeneratorDriver;

        var runResult = driver.GetRunResult();

        return (outputCompilation, runResult);
    }


    // --- Helper Methods (Adapted from Microsoft.CodeAnalysis.Testing) ---

    private static AdhocWorkspace CreateWorkspace()
        => new AdhocWorkspace(MefHostServices.DefaultHost);

    private async Task<Project> CreateProjectAsync(CancellationToken cancellationToken)
    {
        var projectId = ProjectId.CreateNewId("TestProject");
        var solution =
            Workspace.Value.CurrentSolution.AddProject(projectId, "TestProject", "TestProject", LanguageName);
        var project = solution.GetProject(projectId)!;

        // Apply compilation and parse options
        var compilationOptions = CreateCompilationOptions();
        var parseOptions = CreateParseOptions();
        project = project
            .WithCompilationOptions(compilationOptions)
            .WithParseOptions(parseOptions);

        // Add references
        var metadataReferences = await ReferenceAssemblies.ResolveAsync(LanguageName, cancellationToken);
        project = project.WithMetadataReferences(metadataReferences);
        
        // Update the solution to the current value
        solution = project.Solution;

        // Add sources
        int documentIndex = 0;
        foreach (var (filename, content) in TestSources)
        {
            var documentId = DocumentId.CreateNewId(projectId, debugName: filename);
            var sourceText = GetSourceText(content, filename);
            var fullPath = "/" + filename; // Use simple root pathing for tests
            solution = solution.AddDocument(documentId, filename, sourceText, filePath: fullPath);
            documentIndex++;
        }

        // Add analyzer config files
        int configIndex = 0;
        foreach (var (path, content) in AnalyzerConfigFiles)
        {
            var documentId = DocumentId.CreateNewId(projectId, debugName: path);
            var sourceText = GetSourceText(content, path);
            solution = solution.AddAnalyzerConfigDocument(documentId, path, sourceText, filePath: path);
            configIndex++;
        }


        return solution.GetProject(projectId)!;
    }

    private SourceText GetSourceText(object content, string filePath) => content switch
    {
        string text => SourceText.From(text, System.Text.Encoding.UTF8),
        SourceText sourceText => sourceText,
        _ => throw new ArgumentException(
            $"Unsupported source content type: {content.GetType()} for file {filePath}", nameof(content)),
    };

    // --- Nested Helper Class for AnalyzerConfigOptions ---

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<string, string> _globalOptions;

        public TestAnalyzerConfigOptionsProvider(List<(string Path, object Content)> analyzerConfigFiles)
        {
            // Basic parsing for global options (assumes simple key=value lines)
            var globalOptionsBuilder =
                ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, content) in analyzerConfigFiles)
            {
                if (content is string text) // Only process string content for now
                {
                    // Rudimentary parsing - assumes global options are top-level
                    // and ignores section headers for this basic implementation.
                    // A real implementation would need proper .editorconfig parsing.
                    using var reader = new System.IO.StringReader(text);
                    string? line;
                    bool isGlobal = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("[*]") ||
                            line.Contains("is_global = true")) // Crude check for global scope
                        {
                            isGlobal = true;
                        }
                        else if (isGlobal && line.Contains('='))
                        {
                            var parts = line.Split(['='], 2);
                            if (parts.Length == 2)
                            {
                                globalOptionsBuilder[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                        // Add more sophisticated parsing if needed (e.g., for specific file paths)
                    }
                }
            }

            _globalOptions = globalOptionsBuilder.ToImmutable();
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        }
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }

        // This is required by the abstract base class but might not be needed
        // depending on how options are accessed internally by the driver/generator.
        public override IEnumerable<string> Keys => _options.Keys;
    }
}