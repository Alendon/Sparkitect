using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Reflection;
using Sparkitect.Generator.LogEnricher;
using Xunit;

namespace Sparkitect.Generator.Tests;

public class LogEnricherGeneratorTests
{
    [Fact]
    public void GeneratesInterceptorsForLogCalls()
    {
        // Arrange
        var source = @"
using Serilog;

namespace TestNamespace
{
    class TestClass
    {
        public void TestMethod()
        {
            Log.Information(""Test message"");
            Log.Debug(""Test debug {Value}"", 42);
            Log.Error(new System.Exception(""Test""), ""Error occurred"");
        }
    }
}";

        // Act
        var (compilation, output) = GetGeneratedOutput<LogEnricherGenerator>(source);

        // Assert
        Assert.Contains(output, o => o.Contains("namespace Sparkitect.Generated"));
        Assert.Contains(output, o => o.Contains("internal static class LogInterceptors"));
        Assert.Contains(output, o => o.Contains("using (LogContext.PushProperty(\"ModName\", \"Sparkitect\"))"));
        Assert.Contains(output, o => o.Contains("using (LogContext.PushProperty(\"Class\", \"TestClass\"))"));
        Assert.Contains(output, o => o.Contains("Log.Information(messageTemplate)"));
        Assert.Contains(output, o => o.Contains("Log.Debug(messageTemplate, value)"));
        Assert.Contains(output, o => o.Contains("Log.Error(exception, messageTemplate)"));
    }

    [Fact]
    public void HandlesVariousLogMethodOverloads()
    {
        // Arrange
        var source = @"
using Serilog;
using Serilog.Events;

namespace TestNamespace
{
    class TestClass
    {
        public void TestMethod()
        {
            Log.Write(LogEventLevel.Information, ""Test {Value}"", 42);
            Log.Write(LogEventLevel.Error, new System.Exception(""Test""), ""Error {Value}"", 123);
            Log.Fatal(""Critical error"");
        }
    }
}";

        // Act
        var (compilation, output) = GetGeneratedOutput<LogEnricherGenerator>(source);

        // Assert
        Assert.Contains(output, o => o.Contains("LogEventLevel level"));
        Assert.Contains(output, o => o.Contains("Exception exception"));
        Assert.Contains(output, o => o.Contains("Log.Write(level, messageTemplate, value)"));
        Assert.Contains(output, o => o.Contains("Log.Write(level, exception, messageTemplate, value)"));
        Assert.Contains(output, o => o.Contains("Log.Fatal(messageTemplate)"));
    }

    private static (Compilation, ImmutableArray<string>) GetGeneratedOutput<TGenerator>(string source, params string[] additionalSources)
        where TGenerator : IIncrementalGenerator, new()
    {
        // Create syntax tree from source
        var syntaxTrees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(source) };
        syntaxTrees.AddRange(additionalSources.Select(CSharpSyntaxTree.ParseText));

        // Create references for compilation
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(Serilog.Log).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(LogEventLevel).Assembly.Location),
        };

        // Create compilation
        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Create and run the generator
        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        // Get the generated output
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;
        var generatedTexts = generatedTrees.Select(t => t.GetText().ToString()).ToImmutableArray();

        return (compilation, generatedTexts);
    }
}
