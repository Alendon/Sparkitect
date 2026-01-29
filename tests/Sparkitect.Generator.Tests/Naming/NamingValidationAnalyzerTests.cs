using System.Threading.Tasks;
using Sparkitect.Generator.Naming;

namespace Sparkitect.Generator.Tests.Naming;

public sealed class NamingValidationAnalyzerTests : AnalyzerTestBase<NamingValidationAnalyzer>
{
    [Before(Test)]
    public void Setup()
    {
        // Add SnakeCaseAttribute definition for tests
        TestSources.Add(("SnakeCaseAttribute.cs", """
            namespace Sparkitect.Utilities;
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class SnakeCaseAttribute : System.Attribute { }
            """));
    }

    [Test]
    public async Task ValidSnakeCase_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("valid_snake_case");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ValidSnakeCase_WithNumbers_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("my_mod_v2");
                    new Api().Register("id_123");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task InvalidSnakeCase_LeadingUnderscore_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("_leading");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task InvalidSnakeCase_TrailingUnderscore_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("trailing_");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task InvalidSnakeCase_ConsecutiveUnderscores_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("double__underscore");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task InvalidSnakeCase_UpperCase_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("PascalCase");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task InvalidSnakeCase_StartsWithNumber_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("123_start");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task InvalidSnakeCase_ContainsDot_ReportsDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("has.dot");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task NonConstantString_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    string id = "PascalCase";
                    new Api().Register(id); // Variable - not validated
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ParameterWithoutAttribute_NoDiagnostic()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register(string id) { } // No [SnakeCase]
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register("PascalCase"); // Not validated
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task NamedArgument_ValidatesCorrectly()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Api
            {
                public void Register([SnakeCase] string id, string desc) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Api().Register(desc: "anything", id: "BadId");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }

    [Test]
    public async Task Constructor_ValidatesArguments()
    {
        var code = """
            using Sparkitect.Utilities;

            public class Thing
            {
                public Thing([SnakeCase] string id) { }
            }

            public class Usage
            {
                public void Test()
                {
                    new Thing("BadId");
                }
            }
            """;

        TestSources.Add(("Test.cs", code));
        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0502", 1);
    }
}
