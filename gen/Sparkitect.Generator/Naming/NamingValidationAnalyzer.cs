using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Naming;

/// <summary>
/// Analyzer that validates snake_case naming for:
/// 1. ModIdentifier MSBuild property (via CompilerVisibleProperty)
/// 2. String arguments to parameters marked with [SnakeCase] attribute
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamingValidationAnalyzer : DiagnosticAnalyzer
{
    private const string SnakeCaseAttributeFullName = "Sparkitect.Utilities.SnakeCaseAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        NamingDiagnostics.ModIdentifierNotSnakeCase,
        NamingDiagnostics.IdentifierNotSnakeCase
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // MSBuild property validation (ModIdentifier)
        context.RegisterCompilationAction(ValidateModIdentifier);

        // Code argument validation ([SnakeCase] parameters)
        context.RegisterOperationAction(ValidateInvocationArguments,
            OperationKind.Invocation, OperationKind.ObjectCreation);
    }

    private static void ValidateModIdentifier(CompilationAnalysisContext ctx)
    {
        // Access ModIdentifier via CompilerVisibleProperty
        if (!ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions
                .TryGetValue("build_property.ModIdentifier", out var modId))
            return;

        if (string.IsNullOrWhiteSpace(modId))
            return;

        if (StringCase.IsStrictSnakeCase(modId))
            return;

        // Report diagnostic (no specific location for csproj property)
        ctx.ReportDiagnostic(Diagnostic.Create(
            NamingDiagnostics.ModIdentifierNotSnakeCase,
            Location.None,
            modId));
    }

    private static void ValidateInvocationArguments(OperationAnalysisContext ctx)
    {
        var arguments = ctx.Operation switch
        {
            IInvocationOperation inv => inv.Arguments,
            IObjectCreationOperation obj => obj.Arguments,
            _ => ImmutableArray<IArgumentOperation>.Empty
        };

        foreach (var arg in arguments)
        {
            // Skip if no parameter info (shouldn't happen in valid code)
            if (arg.Parameter is null)
                continue;

            // Check if parameter has [SnakeCase] attribute
            if (!HasSnakeCaseAttribute(arg.Parameter))
                continue;

            // Only validate constant string values
            // Non-constant (variables, interpolation) are intentionally ignored
            if (arg.Value.ConstantValue is not { HasValue: true, Value: string value })
                continue;

            if (StringCase.IsStrictSnakeCase(value))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                NamingDiagnostics.IdentifierNotSnakeCase,
                arg.Syntax.GetLocation(),
                value));
        }
    }

    private static bool HasSnakeCaseAttribute(IParameterSymbol param)
    {
        return param.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == SnakeCaseAttributeFullName);
    }
}
