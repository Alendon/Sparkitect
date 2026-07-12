using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    private const string RegisterMarkerInterface = "Sparkitect.Modding.IRegisterMarker";

    internal readonly record struct ProviderFileArg(string PropertyName, string FileName);

    /// <summary>
    /// A single constructed-generic entry in a walk's serialized inputs — either one link in a registered
    /// type's base/interface hierarchy, or a provider's return type. Pure string data (no <see cref="ITypeSymbol"/>)
    /// so it can cross the incremental generator's <c>.Combine()</c> boundary; captured where the symbol is
    /// in scope (<see cref="TryBuildProviderCandidate"/>), matched by pure string comparison at the
    /// <see cref="MapProviderCandidateToUnit"/> join stage (the constraint-guided walk).
    /// </summary>
    internal sealed record SerializedHierarchyType(
        string OpenDefinitionFqn,
        ImmutableValueArray<string> TypeArgumentFqns);

    internal sealed record ProviderCandidate(
        string RegistryTypeName,
        string? RegistryNamespace,
        string MethodName,
        string Id,
        bool IsTypeProvider,
        bool IsPropertyProvider,
        string ProviderContainingTypeFullName,
        string ProviderMethodOrTypeName,
        ImmutableValueArray<ProviderFileArg> Files,
        ImmutableValueArray<(string paramType, bool isNullable)> DiParameters,
        bool IsValueTypeProvider = false,
        bool IsRecordProvider = false,
        // Walk inputs captured here (symbol scope) for the pure-string resolvers to consume at the join
        // stage — never an INamedTypeSymbol/ITypeSymbol field; both are serialized strings only.
        ImmutableValueArray<SerializedHierarchyType> RegisteredTypeHierarchy = default!,
        SerializedHierarchyType? ProviderReturnType = null);

    internal static bool TryExtractProviderInfo(AttributeData attribute,
        out string registryTypeName, out string? registryNamespace, out string methodName, out bool isRegisterMarker)
    {
        registryTypeName = string.Empty;
        methodName = string.Empty;
        isRegisterMarker = false;
        registryNamespace = null;

        var attrClass = attribute.AttributeClass;
        if (attrClass is null)
        {
            return false;
        }

        // Determine if attribute implements IRegisterMarker
        isRegisterMarker = attrClass.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == RegisterMarkerInterface);

        // Derive method name from attribute type name
        var name = attrClass.Name;
        methodName = TrimAttributeSuffix(name);

        var containingType = attrClass.ContainingType;
        if (containingType is null) return false;

        registryTypeName = containingType.Name;
        registryNamespace = containingType.ContainingNamespace?.ToDisplayString(DisplayFormats.NamespaceAndType);

        return attribute.AttributeClass is IErrorTypeSymbol || isRegisterMarker;
    }

    private static string TrimAttributeSuffix(string name)
    {
        return name.EndsWith("Attribute") ? name.Substring(0, name.Length - "Attribute".Length) : name;
    }

    internal static ProviderCandidate? TryBuildProviderCandidate(SyntaxNode node, SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (node is not AttributeSyntax attrSyntax) return null;

        // Determine declaration target (method, property or class)
        var decl = attrSyntax.Parent?.Parent;
        if (decl is null) return null;

        ISymbol? targetSymbol = decl switch
        {
            MethodDeclarationSyntax mds => semanticModel.GetDeclaredSymbol(mds, cancellationToken),
            PropertyDeclarationSyntax pds => semanticModel.GetDeclaredSymbol(pds, cancellationToken),
            ClassDeclarationSyntax cds => semanticModel.GetDeclaredSymbol(cds, cancellationToken),
            StructDeclarationSyntax sds => semanticModel.GetDeclaredSymbol(sds, cancellationToken),
            RecordDeclarationSyntax rds => semanticModel.GetDeclaredSymbol(rds, cancellationToken),
            InterfaceDeclarationSyntax ids => semanticModel.GetDeclaredSymbol(ids, cancellationToken),
            _ => null
        };
        if (targetSymbol is null) return null;

        // Find the concrete AttributeData instance that corresponds to this syntax
        var attributeData = targetSymbol.GetAttributes().FirstOrDefault(a =>
            a.ApplicationSyntaxReference?.Span.Equals(attrSyntax.Span) == true);
        if (attributeData is null) return null;

        if (!TryExtractProviderInfo(attributeData, out var registryTypeName, out string? registryNamespace,
                out var methodName, out var isMarker))
            return null;

        // Parse constructor and named args syntactically to be resilient to error types
        if (!TryParseProviderArguments(attrSyntax, out var id, out var files))
            return null;
        if (!StringCase.IsSnakeCase(id)) return null;

        bool isTypeProvider = targetSymbol is INamedTypeSymbol;
        bool isPropertyProvider = targetSymbol is IPropertySymbol;
        bool isValueTypeProvider = targetSymbol is INamedTypeSymbol nameSym && nameSym.IsValueType;
        bool isRecordProvider = targetSymbol is INamedTypeSymbol recSym && recSym.IsRecord;
        string containerFullName;
        string methodOrTypeName;
        var diParamsBuilder = new ImmutableValueArray<(string paramType, bool isNullable)>.Builder();

        // Walk inputs: captured here, while the symbol is still in scope, as pure strings — no
        // ITypeSymbol crosses the .Combine() boundary. Type providers capture the registered type's
        // base/interface hierarchy; value providers (method/property) capture the provider's return type.
        var registeredTypeHierarchy = new ImmutableValueArray<SerializedHierarchyType>.Builder()
            .ToImmutableValueArray();
        SerializedHierarchyType? providerReturnType = null;

        if (targetSymbol is IMethodSymbol ms)
        {
            containerFullName = ms.ContainingType.ToDisplayString(DisplayFormats.NamespaceAndType);
            methodOrTypeName = ms.Name;

            foreach (var p in ms.Parameters)
            {
                var typeName = p.Type.ToDisplayString(DisplayFormats.NamespaceAndType);
                var isNullable = p.NullableAnnotation == NullableAnnotation.Annotated;
                diParamsBuilder.Add((typeName, isNullable));
            }

            if (ms.ReturnType is INamedTypeSymbol msReturnNamed)
            {
                providerReturnType = SerializeConstructedGeneric(msReturnNamed);
            }
        }
        else if (targetSymbol is IPropertySymbol ps)
        {
            containerFullName = ps.ContainingType.ToDisplayString(DisplayFormats.NamespaceAndType);
            methodOrTypeName = ps.Name;

            if (ps.Type is INamedTypeSymbol psTypeNamed)
            {
                providerReturnType = SerializeConstructedGeneric(psTypeNamed);
            }
        }
        else if (targetSymbol is INamedTypeSymbol nts)
        {
            containerFullName = nts.ContainingNamespace?.ToDisplayString() is { Length: > 0 } ns
                ? ns
                : string.Empty;
            methodOrTypeName = nts.ToDisplayString(DisplayFormats.NamespaceAndType);

            registeredTypeHierarchy = SerializeTypeHierarchy(nts);
        }
        else
        {
            return null;
        }

        return new ProviderCandidate(
            registryTypeName,
            registryNamespace,
            methodName,
            id,
            isTypeProvider,
            isPropertyProvider,
            containerFullName,
            methodOrTypeName,
            files,
            diParamsBuilder.ToImmutableValueArray(),
            isValueTypeProvider,
            isRecordProvider,
            registeredTypeHierarchy,
            providerReturnType);
    }

    /// <summary>
    /// Walks a registered type's <c>BaseType</c> chain (stopping at <c>object</c>) plus its full flattened
    /// <c>AllInterfaces</c> set, serializing every constructed-generic hierarchy link as a
    /// <see cref="SerializedHierarchyType"/>. Non-generic bases/interfaces are skipped — a constraint
    /// reference (<see cref="RegisterConstraintRef"/>) is only ever built for a constructed-generic
    /// constraint, so a non-generic hierarchy entry could never match one.
    /// </summary>
    private static ImmutableValueArray<SerializedHierarchyType> SerializeTypeHierarchy(INamedTypeSymbol nts)
    {
        var builder = new ImmutableValueArray<SerializedHierarchyType>.Builder();

        var current = nts.BaseType;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            if (current.IsGenericType)
            {
                builder.Add(SerializeConstructedGeneric(current));
            }

            current = current.BaseType;
        }

        foreach (var iface in nts.AllInterfaces)
        {
            if (iface.IsGenericType)
            {
                builder.Add(SerializeConstructedGeneric(iface));
            }
        }

        return builder.ToImmutableValueArray();
    }

    /// <summary>
    /// Serializes a named type as a <see cref="SerializedHierarchyType"/>: its open generic definition FQN
    /// (identical to itself for a non-generic type) plus each closed type argument's FQN. Shared by the
    /// hierarchy walk (generic entries only) and the provider-return-type capture (any named type, including
    /// non-generic — a bare-<c>T</c> value method resolves directly to this open FQN with zero arguments).
    /// </summary>
    private static SerializedHierarchyType SerializeConstructedGeneric(INamedTypeSymbol namedType)
    {
        var openFqn = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var argsBuilder = new ImmutableValueArray<string>.Builder();
        foreach (var arg in namedType.TypeArguments)
        {
            argsBuilder.Add(arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return new SerializedHierarchyType(openFqn, argsBuilder.ToImmutableValueArray());
    }

    internal static RegistrationUnit? MapProviderCandidateToUnit(ProviderCandidate cand, RegistryMap regMap)
    {
        if (!regMap.TryGetByFullName(cand.RegistryTypeName, cand.RegistryNamespace, out var model) || model is null)
            return null;

        // Map property names ({PascalCase(Key)}File) to actual keys
        var propToId = new Dictionary<string, string>();
        foreach (var rf in model.ResourceFiles)
        {
            propToId[StringCase.ToPascalCase(rf.Key) + "File"] = rf.Key;
        }

        var filesBuilder = new ImmutableValueArray<(string fileId, string fileName)>.Builder();
        foreach (var f in cand.Files)
        {
            if (propToId.TryGetValue(f.PropertyName, out var id))
            {
                filesBuilder.Add((id, f.FileName));
            }
        }

        var files = filesBuilder.OrderBy(f => f.fileId).ToImmutableValueArray();

        // Resolution head (open-by-construction seam): looked up once here at the join stage.
        // Runs IDENTICALLY whether extracted same-compilation or parsed from external registry metadata —
        // both produce a field-identical RegisterMethodModel.
        var registerMethod = model.RegisterMethods.FirstOrDefault(m => m.FunctionName == cand.MethodName);
        var emptyResolvedArgs = new ImmutableValueArray<string>.Builder().ToImmutableValueArray();

        RegistrationEntry entry;
        // Backward-coordinate identity kept SEPARATE from the concatenated ProviderFullName
        // The typeof target is the CONTAINING type, the member is named on the side.
        var registeredContainer = $"global::{cand.ProviderContainingTypeFullName}";
        if (cand.IsPropertyProvider)
        {
            var providerFull = $"global::{cand.ProviderContainingTypeFullName}.{cand.ProviderMethodOrTypeName}";
            var resolvedArgs = registerMethod is not null && cand.ProviderReturnType is not null
                ? ResolveValueSourceGenericArguments(cand.ProviderReturnType, registerMethod)
                : emptyResolvedArgs;
            entry = new PropertyRegistrationEntry(cand.Id, files, cand.MethodName, providerFull,
                registeredContainer, cand.ProviderMethodOrTypeName, resolvedArgs);
        }
        else if (cand.IsTypeProvider)
        {
            var typeFull = cand.ProviderMethodOrTypeName.StartsWith("global::")
                ? cand.ProviderMethodOrTypeName
                : $"global::{cand.ProviderMethodOrTypeName}";

            KeyedFactoryGenerationInfo? kfg = null;
            if (registerMethod is not null
                && registerMethod.PrimaryParameterKind == PrimaryParameterKind.Type
                && registerMethod.KeyedFactoryMarkerTBase is { } tBase)
            {
                var configuratorClassName =
                    $"{model.TypeName}_{registerMethod.FunctionName}_KeyedFactoryConfigurator";
                kfg = new KeyedFactoryGenerationInfo(tBase, configuratorClassName);
            }

            var typeKind = (cand.IsValueTypeProvider, cand.IsRecordProvider) switch
            {
                (true, true) => RegistrationTypeKind.RecordStruct,
                (false, true) => RegistrationTypeKind.Record,
                (true, false) => RegistrationTypeKind.Struct,
                (false, false) => RegistrationTypeKind.Class,
            };

            var resolvedTypeArgs = registerMethod is not null
                ? ResolveTypeSourceGenericArguments(cand.RegisteredTypeHierarchy, registerMethod, typeFull)
                : emptyResolvedArgs;

            entry = new TypeRegistrationEntry(cand.Id, files, cand.MethodName, typeFull, kfg, typeKind,
                resolvedTypeArgs);
        }
        else
        {
            var providerFull = $"global::{cand.ProviderContainingTypeFullName}.{cand.ProviderMethodOrTypeName}";
            var resolvedArgs = registerMethod is not null && cand.ProviderReturnType is not null
                ? ResolveValueSourceGenericArguments(cand.ProviderReturnType, registerMethod)
                : emptyResolvedArgs;
            entry = new MethodRegistrationEntry(cand.Id, files, cand.MethodName, providerFull, cand.DiParameters,
                registeredContainer, cand.ProviderMethodOrTypeName, resolvedArgs);
        }

        var entries = new ImmutableValueArray<RegistrationEntry>.Builder();
        entries.Add(entry);

        return new RegistrationUnit(
            model,
            SourceKind.Provider,
            "Providers",
            entries.ToImmutableValueArray());
    }

    /// <summary>
    /// Pure-string constraint-guided walk (type-source): resolves the closed type-argument list for a
    /// type-source register method by matching its constraint structure against the registered type's
    /// serialized hierarchy. Seeds the anchor slot (<see cref="RegisterMethodModel.TypeParameterNames"/>
    /// index 0) with <paramref name="anchorTypeFqn"/> (the registered type itself), then iterates to a
    /// fixpoint: a constraint declared on an already-resolved type parameter that matches a hierarchy entry
    /// resolves every type parameter that entry's constructed-generic arguments reference. No symbol
    /// parameter — runs identically whether <paramref name="method"/> was extracted same-compilation or
    /// parsed from external registry metadata. Genuinely unresolvable slots (no matching hierarchy entry
    /// ever found) stay empty — fail-loud; the resulting compile error is the loud signal.
    /// </summary>
    internal static ImmutableValueArray<string> ResolveTypeSourceGenericArguments(
        ImmutableValueArray<SerializedHierarchyType> hierarchy, RegisterMethodModel method, string anchorTypeFqn)
    {
        var names = method.TypeParameterNames;
        if (names is null || names.Count == 0)
        {
            return new ImmutableValueArray<string>.Builder().ToImmutableValueArray();
        }

        var resolved = new string?[names.Count];
        resolved[0] = anchorTypeFqn;

        var constraintRefs = method.ConstraintRefs ??
                              new ImmutableValueArray<RegisterConstraintRef>.Builder().ToImmutableValueArray();

        // Iterative fixpoint (bounded by type-parameter count — no unbounded loop, T-61.2-04b): each pass
        // only resolves constraints whose owning type parameter is already resolved, so a multi-hop chain
        // (T1 : Foo<T2>, T2 : Bar<T3>) resolves across successive passes as each link's owner becomes known.
        var progress = true;
        while (progress)
        {
            progress = false;
            foreach (var constraintRef in constraintRefs)
            {
                var owningIndex = IndexOfTypeParameter(names, constraintRef.TypeParameterName);
                if (owningIndex < 0 || resolved[owningIndex] is null) continue;

                var hierarchyMatch = FindHierarchyMatch(hierarchy, constraintRef.ConstraintOpenDefinitionFqn);
                if (hierarchyMatch is null) continue;

                for (var pos = 0; pos < constraintRef.ArgTypeParameterNames.Count; pos++)
                {
                    var argName = constraintRef.ArgTypeParameterNames[pos];
                    if (string.IsNullOrEmpty(argName)) continue;

                    var argIndex = IndexOfTypeParameter(names, argName);
                    if (argIndex < 0 || resolved[argIndex] is not null) continue;
                    if (pos >= hierarchyMatch.TypeArgumentFqns.Count) continue;

                    resolved[argIndex] = hierarchyMatch.TypeArgumentFqns[pos];
                    progress = true;
                }
            }
        }

        var resultBuilder = new ImmutableValueArray<string>.Builder();
        foreach (var r in resolved) resultBuilder.Add(r ?? string.Empty);
        return resultBuilder.ToImmutableValueArray();
    }

    /// <summary>
    /// Pure-string value-source resolution: reads the closed type-argument list from the
    /// provider's already-compiler-inferred return type, mirroring <c>SettingsAccessorGenerator</c>'s walk
    /// (:82-92). A wrapper-generic value parameter (<see cref="RegisterMethodModel.ValueParameterGeneric"/>
    /// present) matches its open definition against <paramref name="returnType"/> and reads each referenced
    /// slot; a bare-<c>T</c> value method (no wrapper — the method's single type parameter IS the value
    /// parameter) resolves that one slot directly to the provider return type's own open FQN.
    /// </summary>
    internal static ImmutableValueArray<string> ResolveValueSourceGenericArguments(
        SerializedHierarchyType? returnType, RegisterMethodModel method)
    {
        var names = method.TypeParameterNames;
        if (names is null || names.Count == 0 || returnType is null)
        {
            return new ImmutableValueArray<string>.Builder().ToImmutableValueArray();
        }

        var resolved = new string?[names.Count];

        if (method.ValueParameterGeneric is { } valueParameterGeneric)
        {
            if (valueParameterGeneric.ConstraintOpenDefinitionFqn == returnType.OpenDefinitionFqn)
            {
                for (var pos = 0; pos < valueParameterGeneric.ArgTypeParameterNames.Count; pos++)
                {
                    var argName = valueParameterGeneric.ArgTypeParameterNames[pos];
                    if (string.IsNullOrEmpty(argName)) continue;

                    var argIndex = IndexOfTypeParameter(names, argName);
                    if (argIndex < 0 || pos >= returnType.TypeArgumentFqns.Count) continue;

                    resolved[argIndex] = returnType.TypeArgumentFqns[pos];
                }
            }
        }
        else if (names.Count == 1)
        {
            resolved[0] = returnType.OpenDefinitionFqn;
        }

        var resultBuilder = new ImmutableValueArray<string>.Builder();
        foreach (var r in resolved) resultBuilder.Add(r ?? string.Empty);
        return resultBuilder.ToImmutableValueArray();
    }

    private static int IndexOfTypeParameter(ImmutableValueArray<string> names, string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] == name) return i;
        }

        return -1;
    }

    private static SerializedHierarchyType? FindHierarchyMatch(
        ImmutableValueArray<SerializedHierarchyType> hierarchy, string openDefinitionFqn)
    {
        if (hierarchy is null) return null;
        foreach (var entry in hierarchy)
        {
            if (entry.OpenDefinitionFqn == openDefinitionFqn) return entry;
        }

        return null;
    }

    internal static bool TryParseProviderArguments(AttributeSyntax attrSyntax, out string id,
        out ImmutableValueArray<ProviderFileArg> files)
    {
        id = string.Empty;
        var builder = new ImmutableValueArray<ProviderFileArg>.Builder();

        var argList = attrSyntax.ArgumentList;
        if (argList is null)
        {
            files = builder.ToImmutableValueArray();
            return false;
        }

        // Positional first argument is the ID
        foreach (var arg in argList.Arguments)
        {
            if (arg.NameEquals is null && arg.NameColon is null)
            {
                if (arg.Expression is LiteralExpressionSyntax lit &&
                    lit.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression))
                {
                    id = lit.Token.ValueText;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            files = builder.ToImmutableValueArray();
            return false;
        }

        // Named arguments represent files
        foreach (var arg in argList.Arguments)
        {
            if (arg.NameEquals is { } nameEq)
            {
                var prop = nameEq.Name.Identifier.ValueText;
                if (arg.Expression is LiteralExpressionSyntax lit &&
                    lit.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression))
                {
                    var value = lit.Token.ValueText;
                    if (!string.IsNullOrWhiteSpace(value))
                        builder.Add(new ProviderFileArg(prop, value));
                }
            }
        }

        files = builder.ToImmutableValueArray();
        return true;
    }

    // ── Task 2b: Branch B — symbol-driven _KeyedFactory.g.cs per marker-flagged concrete ──

    /// <summary>
    /// Intermediate string-typed candidate record returned by TryBuildMarkerProviderConcrete.
    /// ALL fields are string-typed or value-typed — NO INamedTypeSymbol fields cross the pipeline boundary.
    /// FactoryModel is computed inside the lambda (symbol boundary) before being stored here.
    /// </summary>
    internal sealed record MarkerCandidate(
        string ConcreteFullName,
        string MethodName,
        string ContainingRegistryTypeName,
        string? ContainingRegistryNamespace,
        FactoryModel Factory);

    /// <summary>
    /// Final value record flowing through markerConcreteProvider after joining with RegistryMap.
    /// All fields string-typed or value-typed — incremental-cacheable (no ISymbol fields).
    /// </summary>
    internal sealed record MarkerProviderConcrete(
        string ConcreteTypeFullName,
        string TBaseFullName,
        FactoryModel Factory);

    /// <summary>
    /// SyntaxProvider transform: inspects an AttributeSyntax node, determines if it targets a
    /// class/struct, resolves the concrete type, and calls DiPipeline.ExtractFactory inline
    /// while the INamedTypeSymbol is in scope (never escapes this method).
    /// Returns null if this attribute is not a registry-method provider attribute targeting a class.
    /// </summary>
    internal static MarkerCandidate? TryBuildMarkerProviderConcrete(
        SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (node is not AttributeSyntax attrSyntax) return null;

        // Only care about class/struct/record declarations (type providers)
        var decl = attrSyntax.Parent?.Parent;
        if (decl is not (ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax)) return null;

        ISymbol? targetSymbol = decl switch
        {
            ClassDeclarationSyntax cds => semanticModel.GetDeclaredSymbol(cds, cancellationToken),
            StructDeclarationSyntax sds => semanticModel.GetDeclaredSymbol(sds, cancellationToken),
            RecordDeclarationSyntax rds => semanticModel.GetDeclaredSymbol(rds, cancellationToken),
            _ => null
        };

        if (targetSymbol is not INamedTypeSymbol concreteSymbol) return null;

        // Find the attribute data for this attribute syntax
        var attributeData = targetSymbol.GetAttributes().FirstOrDefault(a =>
            a.ApplicationSyntaxReference?.Span.Equals(attrSyntax.Span) == true);
        if (attributeData is null) return null;

        // Attribute must be a provider marker (implements IRegisterMarker or is error type)
        if (!TryExtractProviderInfo(attributeData, out var registryTypeName, out var registryNamespace,
                out var methodName, out _))
            return null;

        // Parse the ID argument syntactically to avoid error-type issues
        if (!TryParseProviderArguments(attrSyntax, out var id, out _)) return null;
        if (!StringCase.IsSnakeCase(id)) return null;

        // Derive a placeholder TBase — actual TBase resolved in ResolveMarkerConcrete from RegistryMap
        // We use a temporary intent; the actual key expression uses IdentificationHelper.Read<T>()
        var concreteFullName = concreteSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var keyExpr = $"global::Sparkitect.Modding.IdentificationHelper.Read<{concreteFullName}>()";

        // Placeholder TBase — will be resolved in ResolveMarkerConcrete
        // We still call ExtractFactory here while we have the symbol, with a placeholder baseType.
        // ResolveMarkerConcrete will discard this if the method isn't marker-flagged.
        var factory = DiPipeline.ExtractFactory(
            concreteSymbol,
            new FactoryIntent.Keyed(keyExpr, IsRawExpression: true),
            "Sparkitect.Modding.IHasIdentification");  // placeholder; replaced in ResolveMarkerConcrete

        if (factory is null) return null;

        return new MarkerCandidate(concreteFullName, methodName, registryTypeName, registryNamespace, factory);
    }

    /// <summary>
    /// Combine-Select step: given a MarkerCandidate and the RegistryMap, checks if the referenced
    /// registry method is marker-flagged. If yes, rebuilds the FactoryModel with the correct TBase
    /// and returns a MarkerProviderConcrete. Returns null if not marker-flagged.
    /// </summary>
    internal static MarkerProviderConcrete? ResolveMarkerConcrete(
        MarkerCandidate candidate, RegistryMap registryMap)
    {
        if (!registryMap.TryGetByFullName(candidate.ContainingRegistryTypeName, candidate.ContainingRegistryNamespace, out var model) || model is null)
            return null;

        var registerMethod = model.RegisterMethods.FirstOrDefault(m => m.FunctionName == candidate.MethodName);
        if (registerMethod is null) return null;
        if (registerMethod.PrimaryParameterKind != PrimaryParameterKind.Type) return null;
        if (registerMethod.KeyedFactoryMarkerTBase is not { } tBase) return null;

        // Rebuild FactoryModel with the correct TBase (replacing the placeholder used in TryBuildMarkerProviderConcrete)
        var tBaseWithoutGlobal = tBase.StartsWith("global::") ? tBase.Substring("global::".Length) : tBase;
        var factory = candidate.Factory with { BaseType = tBaseWithoutGlobal };

        return new MarkerProviderConcrete(candidate.ConcreteFullName, tBase, factory);
    }
}
