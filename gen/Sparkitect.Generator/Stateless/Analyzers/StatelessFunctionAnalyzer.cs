using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Sparkitect.Generator.Stateless.Analyzers.StatelessDiagnostics;

namespace Sparkitect.Generator.Stateless.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StatelessFunctionAnalyzer : DiagnosticAnalyzer
{
    private const string StatelessFunctionAttributeBase = "Sparkitect.Stateless.StatelessFunctionAttribute";
    private const string SchedulingAttributeBase = "Sparkitect.Stateless.SchedulingAttribute";
    private const string IHasIdentificationInterface = "Sparkitect.Modding.IHasIdentification";
    private const string ParentIdAttributeBase = "Sparkitect.Stateless.ParentIdAttribute";
    private const string AllowConcreteResolutionAttributeFqn = "Sparkitect.DI.GeneratorAttributes.AllowConcreteResolutionAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        MethodMustBeStatic,
        MultipleSchedulingAttributes,
        ParameterNotDiResolvable,
        MissingIHasIdentification,
        NonPublicStaticAccess
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateStatelessFunction, SymbolKind.Method);
        context.RegisterOperationAction(ValidateFieldAccess, OperationKind.FieldReference);
        context.RegisterOperationAction(ValidatePropertyAccess, OperationKind.PropertyReference);
    }

    private static Location? GetAttributeLocation(AttributeData attr)
    {
        return attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
    }

    private void ValidateStatelessFunction(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Find StatelessFunctionAttribute (or derived)
        AttributeData? statelessFuncAttr = null;
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (InheritsFrom(attr.AttributeClass, StatelessFunctionAttributeBase))
            {
                statelessFuncAttr = attr;
                break;
            }
        }

        // If no StatelessFunctionAttribute found, return early
        if (statelessFuncAttr is null)
            return;

        var attrLocation = GetAttributeLocation(statelessFuncAttr) ?? methodSymbol.Locations.FirstOrDefault();

        // SPARK0401: Check if method is static
        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MethodMustBeStatic,
                attrLocation,
                methodSymbol.Name));
        }

        // SPARK0404: Check containing type implements IHasIdentification OR method has ParentIdAttribute
        var containingType = methodSymbol.ContainingType;
        if (containingType is not null)
        {
            var hasIHasIdentification = containingType.AllInterfaces.Any(i =>
                i.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationInterface);

            var hasParentIdAttribute = methodSymbol.GetAttributes().Any(attr =>
                InheritsFrom(attr.AttributeClass, ParentIdAttributeBase));

            if (!hasIHasIdentification && !hasParentIdAttribute)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingIHasIdentification,
                    attrLocation,
                    methodSymbol.Name,
                    containingType.Name));
            }
        }

        // Find all SchedulingAttribute instances
        var schedulingAttributes = methodSymbol.GetAttributes()
            .Where(attr => FindGenericBase(attr.AttributeClass, SchedulingAttributeBase) is not null)
            .ToList();

        // SPARK0402: If count > 1, report multiple scheduling attributes
        if (schedulingAttributes.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MultipleSchedulingAttributes,
                attrLocation,
                methodSymbol.Name));
        }

        // SPARK0403: Check each parameter for DI-resolvability
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol paramType)
                continue;

            // Skip if parameter type is interface
            if (paramType.TypeKind == TypeKind.Interface)
                continue;

            // Skip if parameter type is abstract class
            if (paramType.IsAbstract)
                continue;

            // Skip if parameter has NullableAnnotation.Annotated
            if (parameter.NullableAnnotation == NullableAnnotation.Annotated)
                continue;

            // Skip if parameter type has [AllowConcreteResolution]
            if (paramType.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == AllowConcreteResolutionAttributeFqn))
                continue;

            // Otherwise warn
            context.ReportDiagnostic(Diagnostic.Create(
                ParameterNotDiResolvable,
                parameter.Locations.FirstOrDefault(),
                parameter.Name,
                paramType.ToDisplayString()));
        }
    }

    private static void ValidateFieldAccess(OperationAnalysisContext context)
    {
        if (context.Operation is not IFieldReferenceOperation op)
            return;

        var field = op.Field;
        if (!field.IsStatic) return;
        if (field.IsConst) return; // only const is exempt
        if (!IsInScopeAccessibility(field)) return;
        if (!IsInStatelessFunctionBody(context.ContainingSymbol, out var method)) return;

        context.ReportDiagnostic(Diagnostic.Create(
            NonPublicStaticAccess,
            context.Operation.Syntax.GetLocation(),
            method.Name,
            "field",
            field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static void ValidatePropertyAccess(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation op)
            return;

        var property = op.Property;
        if (!property.IsStatic) return;
        if (!IsInScopeAccessibility(property)) return;
        if (!IsInStatelessFunctionBody(context.ContainingSymbol, out var method)) return;

        context.ReportDiagnostic(Diagnostic.Create(
            NonPublicStaticAccess,
            context.Operation.Syntax.GetLocation(),
            method.Name,
            "property",
            property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool IsInScopeAccessibility(ISymbol symbol)
    {
        // In-scope = private, internal, private protected, file-scoped containing type.
        // Out-of-scope = public, protected, protected internal.
        if (symbol.ContainingType?.IsFileLocal == true) return true;
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => true,
            Accessibility.Internal => true,
            Accessibility.ProtectedAndInternal => true, // C# "private protected"
            Accessibility.Public => false,
            Accessibility.Protected => false,
            Accessibility.ProtectedOrInternal => false, // C# "protected internal"
            _ => false,
        };
    }

    private static bool IsInStatelessFunctionBody(ISymbol? containingSymbol, out IMethodSymbol method)
    {
        method = null!;
        ISymbol? current = containingSymbol;
        while (current is not null)
        {
            if (current is IMethodSymbol m && m.MethodKind == MethodKind.Ordinary)
            {
                method = m;
                break;
            }
            current = current.ContainingSymbol;
        }
        if (method is null) return false;
        foreach (var attr in method.GetAttributes())
        {
            if (InheritsFrom(attr.AttributeClass, StatelessFunctionAttributeBase))
                return true;
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol? type, string baseTypeName)
    {
        while (type is not null)
        {
            if (type.ToDisplayString(DisplayFormats.NamespaceAndType) == baseTypeName)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    private static INamedTypeSymbol? FindGenericBase(INamedTypeSymbol? type, string genericBaseName)
    {
        while (type is not null)
        {
            if (type.IsGenericType &&
                type.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType.WithGenericsOptions(SymbolDisplayGenericsOptions.None)) == genericBaseName)
                return type;
            type = type.BaseType;
        }
        return null;
    }
}
