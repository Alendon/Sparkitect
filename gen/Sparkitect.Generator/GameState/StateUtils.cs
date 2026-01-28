using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparkitect.Generator.GameState;

/// <summary>
/// Utility class providing helper methods for state management code generation.
/// Contains logic for analyzing state attributes, determining schedules, and validating symbols.
/// </summary>
public static class StateUtils
{
    // Attribute metadata names
    internal const string StateModuleInterface = "Sparkitect.GameState.IStateModule";
    internal const string StateDescriptorInterface = "Sparkitect.GameState.IStateDescriptor";

    internal const string StateFunctionAttribute = "Sparkitect.GameState.StateFunctionAttribute";

    // Scheduling attributes
    internal const string PerFrameAttribute = "Sparkitect.GameState.PerFrameAttribute";
    internal const string OnCreateAttribute = "Sparkitect.GameState.OnCreateAttribute";
    internal const string OnDestroyAttribute = "Sparkitect.GameState.OnDestroyAttribute";
    internal const string OnFrameEnterAttribute = "Sparkitect.GameState.OnFrameEnterAttribute";
    internal const string OnFrameExitAttribute = "Sparkitect.GameState.OnFrameExitAttribute";

    // Ordering attributes
    internal const string OrderBeforeAttribute = "Sparkitect.GameState.OrderBeforeAttribute";
    internal const string OrderAfterAttribute = "Sparkitect.GameState.OrderAfterAttribute";
    internal const string OrderBeforeGenericAttribute = "Sparkitect.GameState.OrderBeforeAttribute`1";
    internal const string OrderAfterGenericAttribute = "Sparkitect.GameState.OrderAfterAttribute`1";

    // Module ordering attributes
    internal const string OrderModuleBeforeAttribute = "Sparkitect.GameState.OrderModuleBeforeAttribute`1";
    internal const string OrderModuleAfterAttribute = "Sparkitect.GameState.OrderModuleAfterAttribute`1";

    // Facade attributes
    internal const string StateFacadeAttribute = "Sparkitect.GameState.StateFacadeAttribute`1";
    internal const string RegistryFacadeAttribute = "Sparkitect.Modding.RegistryFacadeAttribute`1";
    internal const string FacadeMarkerBase = "Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute";

    /// <summary>
    /// Determines if a type implements IStateModule
    /// </summary>
    internal static bool IsStateModule(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == StateModuleInterface);
    }

    /// <summary>
    /// Determines if a type implements IStateDescriptor
    /// </summary>
    internal static bool IsStateDescriptor(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == StateDescriptorInterface);
    }

    /// <summary>
    /// Gets the scheduling attribute on a method and returns the corresponding schedule
    /// </summary>
    internal static StateMethodSchedule? GetScheduleFromAttributes(IMethodSymbol method)
    {
        var attributes = method.GetAttributes();

        foreach (var attr in attributes)
        {
            var attrName = attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType);

            switch (attrName)
            {
                case PerFrameAttribute:
                    return StateMethodSchedule.PerFrame;
                case OnCreateAttribute:
                    return StateMethodSchedule.OnCreate;
                case OnDestroyAttribute:
                    return StateMethodSchedule.OnDestroy;
                case OnFrameEnterAttribute:
                    return StateMethodSchedule.OnFrameEnter;
                case OnFrameExitAttribute:
                    return StateMethodSchedule.OnFrameExit;
                default:
                    continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the StateFunction attribute data from a method
    /// </summary>
    internal static AttributeData? GetStateFunctionAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) == StateFunctionAttribute);
    }

    /// <summary>
    /// Extracts the function key from a StateFunction attribute
    /// </summary>
    internal static string? GetFunctionKey(AttributeData stateFunctionAttr)
    {
        if (stateFunctionAttr.ConstructorArguments.Length > 0)
        {
            var keyArg = stateFunctionAttr.ConstructorArguments[0];
            if (keyArg.Value is string key)
                return key;
        }

        return null;
    }

    /// <summary>
    /// Analyzes the key argument of a StateFunction attribute to determine if it's a literal or const reference.
    /// </summary>
    /// <param name="stateFunctionAttr">The StateFunction attribute data</param>
    /// <param name="containingType">The type containing the method</param>
    /// <returns>
    /// IsLiteral: true if the argument is a string literal, false if it references a const field.
    /// ConstFieldReference: If not a literal, the expression to reference the const (e.g., "ModuleName.KeyField").
    /// </returns>
    internal static (bool IsLiteral, string? ConstFieldReference) AnalyzeKeyArgument(
        AttributeData stateFunctionAttr,
        INamedTypeSymbol containingType)
    {
        var syntaxRef = stateFunctionAttr.ApplicationSyntaxReference;
        if (syntaxRef is null)
            return (true, null);

        var syntax = syntaxRef.GetSyntax();
        if (syntax is not AttributeSyntax attrSyntax)
            return (true, null);

        var args = attrSyntax.ArgumentList?.Arguments;
        if (args is null || args.Value.Count == 0)
            return (true, null);

        var firstArg = args.Value[0].Expression;

        // Check if it's a string literal
        if (firstArg is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression })
            return (true, null);

        // Not a literal - extract the const field reference expression
        var constRef = firstArg.ToString();

        // If it's a simple identifier (e.g., InitKey), qualify it with the containing type
        if (firstArg is IdentifierNameSyntax)
        {
            constRef = $"global::{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{constRef}";
        }
        else if (firstArg is MemberAccessExpressionSyntax memberAccess)
        {
            // Already qualified (e.g., OtherModule.SomeKey) - make it global
            constRef = $"global::{memberAccess}";
        }

        return (false, constRef);
    }

    /// <summary>
    /// Checks if a type is a facade type (has StateFacade or RegistryFacade attribute)
    /// </summary>
    internal static bool IsFacadeType(ITypeSymbol type, Compilation compilation)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        var attributes = namedType.GetAttributes();

        foreach (var attr in attributes)
        {
            if (attr.AttributeClass is null) continue;

            // Check if attribute inherits from FacadeMarkerAttribute<T>
            foreach (var baseType in GetBaseTypesAndSelf(attr.AttributeClass))
            {
                if (baseType.ConstructedFrom?.ToDisplayString(DisplayFormats.NamespaceAndType) == FacadeMarkerBase)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all base types of a type including the type itself
    /// </summary>
    private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> GetBaseTypesAndSelf(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Checks if a type is abstract or an interface
    /// </summary>
    internal static bool IsAbstractOrInterface(ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface || type.IsAbstract;
    }

    /// <summary>
    /// Gets ordering constraints from a method's attributes.
    /// Extracts expressions referencing const key fields from syntax.
    /// </summary>
    internal static System.Collections.Generic.IEnumerable<OrderingConstraint> GetOrderingConstraints(
        IMethodSymbol method,
        INamedTypeSymbol containingType,
        Compilation compilation)
    {
        var attributes = method.GetAttributes();

        foreach (var attr in attributes)
        {
            var attrName = attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType);
            var isGeneric = attr.AttributeClass?.IsGenericType ?? false;

            OrderingDirection? direction = null;
            string? targetKeyExpression = null;
            string? targetModuleOrStateType = null;

            // Determine direction
            if (attrName == OrderBeforeAttribute || (isGeneric && attr.AttributeClass?.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) == OrderBeforeGenericAttribute))
            {
                direction = OrderingDirection.Before;
            }
            else if (attrName == OrderAfterAttribute || (isGeneric && attr.AttributeClass?.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType) == OrderAfterGenericAttribute))
            {
                direction = OrderingDirection.After;
            }

            if (direction is null)
                continue;

            // Extract target key expression from syntax (const field reference)
            targetKeyExpression = ExtractKeyExpressionFromAttribute(attr, containingType, compilation);

            // If generic, extract the type argument for cross-module ordering
            if (isGeneric && attr.AttributeClass?.TypeArguments.Length > 0)
            {
                var typeArg = attr.AttributeClass.TypeArguments[0];
                targetModuleOrStateType = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ".Identification";
            }

            if (targetKeyExpression is not null)
            {
                yield return new OrderingConstraint(direction.Value, targetKeyExpression, targetModuleOrStateType);
            }
        }
    }

    /// <summary>
    /// Extracts the key expression from an ordering attribute's constructor argument.
    /// Returns a fully qualified expression for const field references, null for invalid usages.
    /// </summary>
    private static string? ExtractKeyExpressionFromAttribute(
        AttributeData attr,
        INamedTypeSymbol containingType,
        Compilation compilation)
    {
        var syntaxRef = attr.ApplicationSyntaxReference;
        if (syntaxRef is null)
            return null;

        var syntax = syntaxRef.GetSyntax();
        if (syntax is not AttributeSyntax attrSyntax)
            return null;

        var args = attrSyntax.ArgumentList?.Arguments;
        if (args is null || args.Value.Count == 0)
            return null;

        var firstArg = args.Value[0].Expression;

        // String literals are no longer allowed - must use const field reference
        if (firstArg is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression })
        {
            // Skip string literals - analyzer will report error
            return null;
        }

        // Simple identifier (e.g., VulkanInit_Key) - qualify with containing type
        if (firstArg is IdentifierNameSyntax identifier)
        {
            var typeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"{typeName}.{identifier.Identifier.Text}";
        }

        // Member access (e.g., OtherModule.SomeKey) - resolve and fully qualify
        if (firstArg is MemberAccessExpressionSyntax memberAccess)
        {
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            var targetSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;

            if (targetSymbol is INamedTypeSymbol targetType)
            {
                var qualifiedTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{qualifiedTypeName}.{memberAccess.Name.Identifier.Text}";
            }

            // Fallback: skip if we can't resolve the target type
            return null;
        }

        return null;
    }

    /// <summary>
    /// Gets module ordering constraints (OrderModuleBefore/After) from a type
    /// </summary>
    internal static (System.Collections.Generic.IEnumerable<string> Before, System.Collections.Generic.IEnumerable<string> After) GetModuleOrderingConstraints(INamedTypeSymbol moduleType)
    {
        var beforeTypes = new System.Collections.Generic.List<string>();
        var afterTypes = new System.Collections.Generic.List<string>();

        var attributes = moduleType.GetAttributes();

        foreach (var attr in attributes)
        {
            if (attr.AttributeClass is null || !attr.AttributeClass.IsGenericType)
                continue;

            var constructedFrom = attr.AttributeClass.ConstructedFrom.ToDisplayString(DisplayFormats.NamespaceAndType);

            if (attr.AttributeClass.TypeArguments.Length > 0)
            {
                var targetType = attr.AttributeClass.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (constructedFrom == OrderModuleBeforeAttribute)
                {
                    beforeTypes.Add(targetType);
                }
                else if (constructedFrom == OrderModuleAfterAttribute)
                {
                    afterTypes.Add(targetType);
                }
            }
        }

        return (beforeTypes, afterTypes);
    }
}