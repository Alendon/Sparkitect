using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.GameState.Diagnostics;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.GameState.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateFunctionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        StateFunctionMissingSchedule,
        StateFunctionMultipleSchedules,
        StateFunctionNotStatic,
        StateFunctionDuplicateKey,
        StateFunctionParameterNotAbstract,
        OrderingInvalidTargetType,
        StateFunctionInvalidKey,
        StateFunctionNotInModule
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateStateModule, SymbolKind.NamedType);
    }

    private void ValidateStateModule(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type) return;

        // Check if type implements IStateModule
        bool isModule = IsStateModule(type);

        // Get all methods in the type
        var methods = type.GetMembers().OfType<IMethodSymbol>();

        // Track keys within this module to detect duplicates
        var keysInModule = new Dictionary<string, IMethodSymbol>();

        foreach (var method in methods)
        {
            var stateFunctionAttr = GetStateFunctionAttribute(method);
            if (stateFunctionAttr is null)
                continue;

            // Validate method is in a module
            if (!isModule)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateFunctionNotInModule,
                    method.Locations.FirstOrDefault(),
                    method.Name));
                continue;
            }

            // Validate key
            var key = GetFunctionKey(stateFunctionAttr);
            if (string.IsNullOrWhiteSpace(key))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateFunctionInvalidKey,
                    method.Locations.FirstOrDefault(),
                    method.Name));
                continue;
            }

            // Check for duplicate keys
            if (keysInModule.TryGetValue(key, out var existingMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateFunctionDuplicateKey,
                    method.Locations.FirstOrDefault(),
                    type.Name,
                    key));
            }
            else
            {
                keysInModule[key] = method;
            }

            // Validate method is static
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    StateFunctionNotStatic,
                    method.Locations.FirstOrDefault(),
                    method.Name));
            }

            // Validate scheduling attributes
            ValidateSchedulingAttributes(context, method);

            // Validate parameters
            foreach (var parameter in method.Parameters)
            {
                ValidateParameter(context, method, parameter);
            }

            // Validate ordering attributes
            ValidateOrderingAttributes(context, method);
        }
    }

    private void ValidateSchedulingAttributes(SymbolAnalysisContext context, IMethodSymbol method)
    {
        var attributes = method.GetAttributes();

        var schedulingAttrs = attributes.Where(attr =>
        {
            var attrName = attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType);
            return attrName == PerFrameAttribute ||
                   attrName == OnStateEnterAttribute ||
                   attrName == OnStateExitAttribute ||
                   attrName == OnModuleEnterAttribute ||
                   attrName == OnModuleExitAttribute;
        }).ToList();

        if (schedulingAttrs.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateFunctionMissingSchedule,
                method.Locations.FirstOrDefault(),
                method.Name));
        }
        else if (schedulingAttrs.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateFunctionMultipleSchedules,
                method.Locations.FirstOrDefault(),
                method.Name));
        }
    }

    private void ValidateParameter(SymbolAnalysisContext context, IMethodSymbol method, IParameterSymbol parameter)
    {
        if (parameter.Type is not INamedTypeSymbol paramType) return;

        // Check if parameter is abstract or interface
        if (!IsAbstractOrInterface(paramType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StateFunctionParameterNotAbstract,
                parameter.Locations.FirstOrDefault(),
                parameter.Name,
                paramType.ToDisplayString()));
        }
    }

    private void ValidateOrderingAttributes(SymbolAnalysisContext context, IMethodSymbol method)
    {
        var attributes = method.GetAttributes();

        foreach (var attr in attributes)
        {
            var attrName = attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType);
            var isGeneric = attr.AttributeClass?.IsGenericType ?? false;

            // Check if it's an ordering attribute
            bool isOrderingAttr =
                attrName == OrderBeforeAttribute ||
                attrName == OrderAfterAttribute ||
                (isGeneric && (attr.AttributeClass?.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) == OrderBeforeGenericAttribute ||
                               attr.AttributeClass?.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) == OrderAfterGenericAttribute));

            if (!isOrderingAttr)
                continue;

            // If generic, validate that the type argument is a valid module or state descriptor
            if (isGeneric && attr.AttributeClass?.TypeArguments.Length > 0)
            {
                var targetType = attr.AttributeClass.TypeArguments[0];

                if (targetType is INamedTypeSymbol namedTargetType)
                {
                    bool isValidTarget = IsStateModule(namedTargetType) || IsStateDescriptor(namedTargetType);

                    if (!isValidTarget)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            OrderingInvalidTargetType,
                            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            method.Name,
                            targetType.ToDisplayString()));
                    }
                }
            }
        }
    }
}