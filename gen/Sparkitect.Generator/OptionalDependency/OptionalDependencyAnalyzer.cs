using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Sparkitect.Generator.OptionalDependency;

/// <summary>
/// Analyzer that detects type leakage from optional mod dependencies.
/// Types from optional mods must only appear within:
/// - Classes marked with [OptionalModDependent("mod_id")]
/// - Methods marked with [ModLoadedGuard("mod_id")]
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionalDependencyAnalyzer : DiagnosticAnalyzer
{
    private const string OptionalModDependentFullName =
        "Sparkitect.Modding.OptionalModDependentAttribute";
    private const string ModLoadedGuardFullName =
        "Sparkitect.Modding.ModLoadedGuardAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        OptionalDependencyDiagnostics.TypeLeakage,
        OptionalDependencyDiagnostics.UnguardedCall,
        OptionalDependencyDiagnostics.InvalidModId
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext ctx)
    {
        // Early exit: no optional assemblies configured
        if (!ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions
                .TryGetValue("build_property.OptionalModAssemblies", out var assembliesValue) ||
            string.IsNullOrWhiteSpace(assembliesValue))
            return;

        var optionalAssemblyNames = new HashSet<string>(
            assembliesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // Build mod ID to assembly name mapping for attribute validation
        // OptionalModIds and OptionalModAssemblies are parallel lists (same index = same mod)
        var modIdToAssembly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var assemblyToModId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions
                .TryGetValue("build_property.OptionalModIds", out var modIdsValue) &&
            !string.IsNullOrWhiteSpace(modIdsValue))
        {
            var modIds = modIdsValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToArray();
            var assemblies = assembliesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToArray();

            for (var i = 0; i < Math.Min(modIds.Length, assemblies.Length); i++)
            {
                modIdToAssembly[modIds[i]] = assemblies[i];
                assemblyToModId[assemblies[i]] = modIds[i];
            }
        }

        // Register symbol analysis with captured context
        ctx.RegisterSymbolAction(
            c => AnalyzeNamedType(c, optionalAssemblyNames, modIdToAssembly, assemblyToModId),
            SymbolKind.NamedType);
        ctx.RegisterSymbolAction(
            c => AnalyzeMethod(c, optionalAssemblyNames, modIdToAssembly, assemblyToModId),
            SymbolKind.Method);

        // Register operation block analysis for method body type references
        ctx.RegisterOperationBlockAction(
            c => AnalyzeOperationBlock(c, optionalAssemblyNames, modIdToAssembly, assemblyToModId));
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext ctx,
        HashSet<string> optionalAssemblyNames,
        Dictionary<string, string> modIdToAssembly,
        Dictionary<string, string> assemblyToModId)
    {
        if (ctx.Symbol is not INamedTypeSymbol type) return;

        // Get allowed assemblies from [OptionalModDependent] on this type
        // The attribute uses mod IDs, which we map to assembly names
        var allowedAssemblies = GetAllowedOptionalAssemblies(type, modIdToAssembly);

        // Check fields (skip compiler-generated backing fields)
        foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsImplicitlyDeclared) continue;
            CheckTypeReference(ctx, field.Type, field.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }

        // Check properties
        foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
        {
            CheckTypeReference(ctx, property.Type, property.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }

        // Check base type
        if (type.BaseType is not null && type.BaseType.SpecialType == SpecialType.None)
        {
            CheckTypeReference(ctx, type.BaseType, type.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }

        // Check interfaces
        foreach (var iface in type.Interfaces)
        {
            CheckTypeReference(ctx, iface, type.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext ctx,
        HashSet<string> optionalAssemblyNames,
        Dictionary<string, string> modIdToAssembly,
        Dictionary<string, string> assemblyToModId)
    {
        if (ctx.Symbol is not IMethodSymbol method) return;

        // Skip compiler-generated methods and property/event accessors
        // (properties are already checked in AnalyzeNamedType)
        if (method.IsImplicitlyDeclared) return;
        if (method.AssociatedSymbol != null) return;

        // Get allowed assemblies from containing type AND method
        // The attributes use mod IDs, which we map to assembly names
        var containingType = method.ContainingType;
        var allowedAssemblies = GetAllowedOptionalAssemblies(containingType, modIdToAssembly);
        allowedAssemblies.UnionWith(GetMethodGuardedAssemblies(method, modIdToAssembly));

        // Check return type
        if (!method.ReturnsVoid)
        {
            CheckTypeReference(ctx, method.ReturnType, method.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }

        // Check parameters
        foreach (var param in method.Parameters)
        {
            CheckTypeReference(ctx, param.Type, param.Locations.FirstOrDefault(),
                optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
        }

        // Check type parameter constraints
        foreach (var typeParam in method.TypeParameters)
        {
            foreach (var constraint in typeParam.ConstraintTypes)
            {
                CheckTypeReference(ctx, constraint, method.Locations.FirstOrDefault(),
                    optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
            }
        }
    }

    private static void AnalyzeOperationBlock(
        OperationBlockAnalysisContext ctx,
        HashSet<string> optionalAssemblyNames,
        Dictionary<string, string> modIdToAssembly,
        Dictionary<string, string> assemblyToModId)
    {
        // Skip compiler-generated symbols (backing fields, etc.)
        if (ctx.OwningSymbol.IsImplicitlyDeclared) return;

        // Get containing type for class-level guards
        var containingType = ctx.OwningSymbol.ContainingType;
        if (containingType == null) return;

        // Build allowed assemblies from class-level [OptionalModDependent]
        var allowedAssemblies = GetAllowedOptionalAssemblies(containingType, modIdToAssembly);

        // If owning symbol is a method, also check for [ModLoadedGuard]
        if (ctx.OwningSymbol is IMethodSymbol method)
        {
            allowedAssemblies.UnionWith(GetMethodGuardedAssemblies(method, modIdToAssembly));
        }

        // Walk ALL operations in ALL operation blocks, check ALL types
        foreach (var operationBlock in ctx.OperationBlocks)
        {
            foreach (var operation in operationBlock.DescendantsAndSelf())
            {
                foreach (var type in GetTypesFromOperation(operation))
                {
                    CheckOperationTypeReference(ctx, type, operation.Syntax.GetLocation(),
                        optionalAssemblyNames, allowedAssemblies, assemblyToModId, modIdToAssembly);
                }
            }
        }
    }

    private static IEnumerable<ITypeSymbol> GetTypesFromOperation(IOperation operation)
    {
        // Result type of any expression
        if (operation.Type != null)
            yield return operation.Type;

        switch (operation)
        {
            // Method invocation - check the method's containing type
            case IInvocationOperation invocation:
                if (invocation.TargetMethod.ContainingType != null)
                    yield return invocation.TargetMethod.ContainingType;
                break;

            // Member reference (property, field, event access) - check containing type
            case IMemberReferenceOperation memberRef:
                if (memberRef.Member.ContainingType != null)
                    yield return memberRef.Member.ContainingType;
                break;

            // Object creation - check the created type
            case IObjectCreationOperation creation:
                if (creation.Type != null)
                    yield return creation.Type;
                break;

            // typeof(T) expressions
            case ITypeOfOperation typeOf:
                if (typeOf.TypeOperand != null)
                    yield return typeOf.TypeOperand;
                break;

            // Cast expressions
            case IConversionOperation conversion:
                if (conversion.Type != null)
                    yield return conversion.Type;
                break;

            // is/as pattern matching
            case IIsPatternOperation isPattern:
                foreach (var type in GetTypesFromPattern(isPattern.Pattern))
                    yield return type;
                break;

            // catch clauses
            case ICatchClauseOperation catchClause:
                if (catchClause.ExceptionType != null)
                    yield return catchClause.ExceptionType;
                break;
        }
    }

    private static IEnumerable<ITypeSymbol> GetTypesFromPattern(IPatternOperation pattern)
    {
        switch (pattern)
        {
            case ITypePatternOperation typePattern:
                if (typePattern.MatchedType != null)
                    yield return typePattern.MatchedType;
                break;

            case IDeclarationPatternOperation declarationPattern:
                if (declarationPattern.MatchedType != null)
                    yield return declarationPattern.MatchedType;
                break;

            case IRecursivePatternOperation recursivePattern:
                if (recursivePattern.MatchedType != null)
                    yield return recursivePattern.MatchedType;
                break;
        }
    }

    private static void CheckOperationTypeReference(
        OperationBlockAnalysisContext ctx,
        ITypeSymbol type,
        Location location,
        HashSet<string> optionalAssemblyNames,
        HashSet<string> allowedAssemblies,
        Dictionary<string, string> assemblyToModId,
        Dictionary<string, string> modIdToAssembly)
    {
        foreach (var referencedType in GetAllTypeReferences(type))
        {
            // Check 1: Type from optional mod assembly
            var assemblyName = referencedType.ContainingAssembly?.Name;
            if (!string.IsNullOrEmpty(assemblyName) &&
                optionalAssemblyNames.Contains(assemblyName) &&
                !allowedAssemblies.Contains(assemblyName))
            {
                var modId = assemblyToModId.TryGetValue(assemblyName, out var id) ? id : assemblyName;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    OptionalDependencyDiagnostics.TypeLeakage,
                    location,
                    referencedType.ToDisplayString(DisplayFormats.NamespaceAndType),
                    modId));
            }

            // Check 2: Type has [OptionalModDependent] - transitive leakage
            foreach (var modId in GetOptionalModDependentModIds(referencedType))
            {
                var modAssembly = modIdToAssembly.TryGetValue(modId, out var name) ? name : modId;
                if (!allowedAssemblies.Contains(modAssembly))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        OptionalDependencyDiagnostics.TypeLeakage,
                        location,
                        referencedType.ToDisplayString(DisplayFormats.NamespaceAndType),
                        modId));
                }
            }
        }
    }

    private static void CheckTypeReference(
        SymbolAnalysisContext ctx,
        ITypeSymbol type,
        Location? location,
        HashSet<string> optionalAssemblyNames,
        HashSet<string> allowedAssemblies,
        Dictionary<string, string> assemblyToModId,
        Dictionary<string, string> modIdToAssembly)
    {
        foreach (var referencedType in GetAllTypeReferences(type))
        {
            // Check 1: Type from optional mod assembly
            var assemblyName = referencedType.ContainingAssembly?.Name;
            if (!string.IsNullOrEmpty(assemblyName) &&
                optionalAssemblyNames.Contains(assemblyName) &&
                !allowedAssemblies.Contains(assemblyName))
            {
                var modId = assemblyToModId.TryGetValue(assemblyName, out var id) ? id : assemblyName;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    OptionalDependencyDiagnostics.TypeLeakage,
                    location ?? Location.None,
                    referencedType.ToDisplayString(DisplayFormats.NamespaceAndType),
                    modId));
            }

            // Check 2: Type has [OptionalModDependent] - transitive leakage
            foreach (var modId in GetOptionalModDependentModIds(referencedType))
            {
                var modAssembly = modIdToAssembly.TryGetValue(modId, out var name) ? name : modId;
                if (!allowedAssemblies.Contains(modAssembly))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        OptionalDependencyDiagnostics.TypeLeakage,
                        location ?? Location.None,
                        referencedType.ToDisplayString(DisplayFormats.NamespaceAndType),
                        modId));
                }
            }
        }
    }

    /// <summary>
    /// Gets mod IDs from [OptionalModDependent] attributes on a type.
    /// Used for detecting transitive type leakage - when a local type depends on an optional mod,
    /// using that type outside a guarded context also requires the guard.
    /// </summary>
    private static IEnumerable<string> GetOptionalModDependentModIds(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) yield break;

        foreach (var attr in namedType.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) != OptionalModDependentFullName)
                continue;

            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string modId)
            {
                yield return modId;
            }
        }
    }

    /// <summary>
    /// Recursively extracts all type references including generic arguments and array element types.
    /// </summary>
    private static IEnumerable<ITypeSymbol> GetAllTypeReferences(ITypeSymbol type)
    {
        yield return type;

        if (type is INamedTypeSymbol named)
        {
            foreach (var arg in named.TypeArguments)
            foreach (var inner in GetAllTypeReferences(arg))
                yield return inner;
        }

        if (type is IArrayTypeSymbol array)
        {
            foreach (var inner in GetAllTypeReferences(array.ElementType))
                yield return inner;
        }
    }

    /// <summary>
    /// Gets assembly names that are allowed in this type context via [OptionalModDependent].
    /// The attribute contains mod IDs which are mapped to assembly names.
    /// </summary>
    private static HashSet<string> GetAllowedOptionalAssemblies(
        INamedTypeSymbol type,
        Dictionary<string, string> modIdToAssembly)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                != OptionalModDependentFullName)
                continue;

            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string modId)
            {
                // Map mod ID to assembly name using the SDK-provided mapping
                if (modIdToAssembly.TryGetValue(modId, out var assemblyName))
                {
                    allowed.Add(assemblyName);
                }
                else
                {
                    // Fallback: use mod ID as-is (for backwards compatibility or tests)
                    allowed.Add(modId);
                }
            }
        }

        return allowed;
    }

    /// <summary>
    /// Gets assembly names that are allowed in this method context via [ModLoadedGuard].
    /// The attribute contains mod IDs which are mapped to assembly names.
    /// </summary>
    private static HashSet<string> GetMethodGuardedAssemblies(
        IMethodSymbol method,
        Dictionary<string, string> modIdToAssembly)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType)
                != ModLoadedGuardFullName)
                continue;

            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string modId)
            {
                // Map mod ID to assembly name using the SDK-provided mapping
                if (modIdToAssembly.TryGetValue(modId, out var assemblyName))
                {
                    allowed.Add(assemblyName);
                }
                else
                {
                    // Fallback: use mod ID as-is (for backwards compatibility or tests)
                    allowed.Add(modId);
                }
            }
        }

        return allowed;
    }
}
