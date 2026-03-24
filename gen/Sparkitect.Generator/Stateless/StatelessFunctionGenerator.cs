using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Generator.Metadata;
using Sparkitect.Generator.Modding;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Stateless;

[Generator]
public class StatelessFunctionGenerator : IIncrementalGenerator
{
    private const string StatelessFunctionAttributeBase = "Sparkitect.Stateless.StatelessFunctionAttribute";
    private const string SchedulingAttributeBase = "Sparkitect.Stateless.SchedulingAttribute";
    private const string IHasIdentificationInterface = "Sparkitect.Modding.IHasIdentification";
    private const string ParentIdAttributeBase = "Sparkitect.Stateless.ParentIdAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildSettings = context.GetModBuildSettings();

        // Scan for methods with attributes, checking for StatelessFunctionAttribute inheritance
        var functionsProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: (syntaxContext, ct) => TryExtractStatelessFunction(syntaxContext, ct)
        ).Where(f => f is not null);

        // Group by parent type
        var parentGroupsProvider = functionsProvider
            .Collect()
            .Select((functions, _) => GroupByParent(functions!));

        // Output wrappers (one per function) - needs ModBuildSettings for ID path
        context.RegisterSourceOutput(functionsProvider.Combine(buildSettings),
            static (ctx, pair) => OutputWrapper(ctx, pair.Left, pair.Right));

        // Output registration and scheduling (grouped by parent + registry)
        context.RegisterSourceOutput(parentGroupsProvider.Combine(buildSettings),
            static (ctx, pair) => OutputRegistrationAndScheduling(ctx, pair.Left, pair.Right));
    }

    private static StatelessFunctionModel? TryExtractStatelessFunction(
        GeneratorSyntaxContext syntaxContext,
        CancellationToken ct)
    {
        if (syntaxContext.Node is not MethodDeclarationSyntax methodSyntax)
            return null;

        var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(methodSyntax, ct);
        if (declaredSymbol is not IMethodSymbol methodSymbol)
            return null;

        // Find StatelessFunctionAttribute (or derived)
        AttributeData? statelessFuncAttr = null;
        INamedTypeSymbol? registryType = null;
        INamedTypeSymbol? contextType = null;
        string? identifier = null;

        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (InheritsFrom(attr.AttributeClass, StatelessFunctionAttributeBase))
            {
                statelessFuncAttr = attr;
                // Extract TContext, TRegistry from StatelessFunctionAttribute<TContext, TRegistry>
                var baseGeneric = FindGenericBase(attr.AttributeClass, "Sparkitect.Stateless.StatelessFunctionAttribute");
                if (baseGeneric is { TypeArguments.Length: 2 })
                {
                    contextType = baseGeneric.TypeArguments[0] as INamedTypeSymbol;
                    registryType = baseGeneric.TypeArguments[1] as INamedTypeSymbol;
                }
                // Extract identifier from constructor
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string id)
                {
                    identifier = id;
                }
                break;
            }
        }

        if (statelessFuncAttr is null || registryType is null || contextType is null || string.IsNullOrEmpty(identifier))
            return null;

        // Extract registry key
        if (!RegistryGenerator.TryExtractRegistryKey(registryType, out var registryKey))
            return null;

        // Find SchedulingAttribute (or derived) -- now via MetadataAttribute<T> base
        INamedTypeSymbol? schedulingType = null;

        foreach (var attr in methodSymbol.GetAttributes())
        {
            var baseGeneric = FindGenericBase(attr.AttributeClass, "Sparkitect.Metadata.MetadataAttribute");
            if (baseGeneric is { TypeArguments.Length: 1 })
            {
                schedulingType = baseGeneric.TypeArguments[0] as INamedTypeSymbol;
                break;
            }
        }

        if (schedulingType is null)
            return null;

        // Get containing type
        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
            return null;

        // Find ParentIdAttribute<T> if present - this overrides the parent identification
        INamedTypeSymbol? parentIdType = null;
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (InheritsFrom(attr.AttributeClass, ParentIdAttributeBase) &&
                attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 })
            {
                parentIdType = attr.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
                break;
            }
        }

        // Check IHasIdentification on containing type
        var hasIHasIdentification = containingType.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationInterface);

        // Either containing type implements IHasIdentification OR method has ParentIdAttribute
        if (!hasIHasIdentification && parentIdType is null)
            return null;

        // Determine the parent identification type (use ParentIdAttribute type if present, else containingType)
        var parentIdentificationType = parentIdType ?? containingType;
        var parentIdentificationTypeName = parentIdentificationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Extract method parameters
        var parameters = methodSymbol.Parameters
            .Select((p, i) => new StatelessParameterModel(
                i,
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                p.NullableAnnotation == NullableAnnotation.Annotated))
            .ToImmutableValueArray();

        // Extract scheduling constructor params and match attributes
        var schedulingParams = ExtractSchedulingParams(schedulingType, methodSymbol);

        var identifierPascal = StringCase.ToPascalCase(identifier);
        var wrapperClassName = $"{identifierPascal}Func";
        var parentFullName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var wrapperFullTypeName = $"{parentFullName}.{wrapperClassName}";

        // Extract facade metadata from method parameters (not from the containing class)
        var facadeResults = new List<FacadeMetadataModel>();
        foreach (var param in methodSymbol.Parameters)
        {
            if (param.Type is INamedTypeSymbol paramType && paramType.TypeKind == TypeKind.Interface)
            {
                DiPipeline.CollectFacadeMappings(paramType, "Sparkitect.GameState.StateFacadeAttribute", facadeResults);
            }
        }
        var facadeMetadata = facadeResults.ToImmutableValueArray();

        return new StatelessFunctionModel(
            methodSymbol.Name,
            identifier,
            identifierPascal,
            wrapperClassName,
            wrapperFullTypeName,
            statelessFuncAttr.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            schedulingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            registryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            registryKey,
            contextType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            parentFullName,
            parentIdentificationTypeName,
            parameters,
            schedulingParams,
            facadeMetadata);
    }

    internal static ImmutableValueArray<SchedulingConstructorParam> ExtractSchedulingParams(
        INamedTypeSymbol schedulingType,
        IMethodSymbol method)
    {
        var metadataParams = MetadataExtractionPipeline.Extract(
            schedulingType, method, ResolveTypeArgument);

        // Convert MetadataConstructorParam -> SchedulingConstructorParam
        var builder = new ImmutableValueArray<SchedulingConstructorParam>.Builder();
        foreach (var mp in metadataParams)
        {
            var instances = new ImmutableValueArray<SchedulingAttributeInstance>.Builder();
            foreach (var mi in mp.Instances)
                instances.Add(new SchedulingAttributeInstance(mi.GenericArgs, mi.CtorArgs));
            builder.Add(new SchedulingConstructorParam(mp.AttributeTypeName, mp.IsNullable, mp.IsArray, instances.ToImmutableValueArray()));
        }
        return builder.ToImmutableValueArray();
    }

    /// <summary>
    /// Resolves a type argument, handling ErrorTypeSymbol for SG-generated wrapper types.
    /// When OrderAfter&lt;WrapperType&gt; references a type generated by this same SG run,
    /// Roslyn returns ErrorTypeSymbol. We deduce the correct globalized name from the
    /// wrapper naming convention ({IdentifierPascal}Func).
    /// </summary>
    private static string ResolveTypeArgument(ITypeSymbol typeArg, ISymbol symbol)
    {
        // Normal case: type resolved successfully
        if (typeArg.TypeKind != TypeKind.Error)
        {
            return typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // ErrorTypeSymbol case: try to deduce wrapper type name
        var errorTypeName = typeArg.Name;

        // Wrapper naming convention: {IdentifierPascal}Func
        if (!errorTypeName.EndsWith("Func"))
        {
            // Not a wrapper reference, fall back to original behavior
            return typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Check if this is a qualified reference (e.g., OtherOwner.OtherMethodFunc)
        // ErrorTypeSymbol.ContainingType will be set for qualified references
        if (typeArg.ContainingType is { } containingErrorType)
        {
            // Qualified form: OtherOwner.OtherMethodFunc
            // The containing type should be resolvable (it's the class containing the referenced method)
            var ownerTypeName = containingErrorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"{ownerTypeName}.{errorTypeName}";
        }

        // Unqualified form: OtherMethodFunc
        // Must be a sibling method in the same class - check if such a method exists
        var containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // The wrapper class is nested in the same type as the method
        var containingTypeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{containingTypeName}.{errorTypeName}";
    }

    internal static string GetNonGenericBaseTypeName(ITypeSymbol? type)
        => MetadataExtractionPipeline.GetNonGenericBaseTypeName(type);

    internal static string FormatTypedConstant(TypedConstant constant)
        => MetadataExtractionPipeline.FormatTypedConstant(constant);

    private static bool InheritsFrom(INamedTypeSymbol? type, string baseTypeName)
        => MetadataExtractionPipeline.InheritsFrom(type, baseTypeName);

    private static INamedTypeSymbol? FindGenericBase(INamedTypeSymbol? type, string genericBaseName)
        => MetadataExtractionPipeline.FindGenericBase(type, genericBaseName);

    private static ImmutableValueArray<StatelessParentModel> GroupByParent(
        IEnumerable<StatelessFunctionModel> functions)
    {
        var builder = new ImmutableValueArray<StatelessParentModel>.Builder();

        var grouped = functions.GroupBy(f => f.ParentTypeName);
        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            var first = group.First();
            // Extract short name and namespace from full type name
            var fullName = first.ParentTypeName;
            var lastDot = fullName.LastIndexOf('.');
            var shortName = lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
            var ns = lastDot >= 0 ? fullName.Substring(0, lastDot) : string.Empty;
            // Strip global:: prefix from namespace
            if (ns.StartsWith("global::"))
                ns = ns.Substring(8);

            builder.Add(new StatelessParentModel(
                shortName,
                ns,
                fullName,
                group.OrderBy(f => f.Identifier).ToImmutableValueArray()));
        }

        return builder.ToImmutableValueArray();
    }

    private static void OutputWrapper(SourceProductionContext context, StatelessFunctionModel? func, ModBuildSettings settings)
    {
        if (func is null) return;

        if (RenderWrapper(func, settings, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }

        // Emit metadata entrypoint if facade metadata was extracted
        if (func.FacadeMetadata.Count > 0)
        {
            // Strip global:: prefix for namespace extraction
            var wrapperFullName = func.WrapperFullTypeName;
            if (wrapperFullName.StartsWith("global::"))
                wrapperFullName = wrapperFullName.Substring(8);

            var lastDot = wrapperFullName.LastIndexOf('.');
            var wrapperNs = lastDot >= 0 ? wrapperFullName.Substring(0, lastDot) : string.Empty;

            var models = func.FacadeMetadata.Cast<IMetadataModel>().ToList();
            if (DiPipeline.RenderMetadataEntrypoint(wrapperFullName, wrapperNs, models, settings,
                    out var metaCode, out var metaFileName))
            {
                context.AddSource(metaFileName, metaCode);
            }
        }
    }

    private static void OutputRegistrationAndScheduling(
        SourceProductionContext context,
        ImmutableValueArray<StatelessParentModel> parents,
        ModBuildSettings settings)
    {
        if (parents.Count == 0) return;

        // Group all functions by registry
        var byRegistry = new Dictionary<string, List<(StatelessParentModel parent, StatelessFunctionModel func)>>();

        foreach (var parent in parents)
        {
            foreach (var func in parent.Functions)
            {
                var key = func.RegistryTypeName;
                if (!byRegistry.TryGetValue(key, out var list))
                {
                    list = new List<(StatelessParentModel, StatelessFunctionModel)>();
                    byRegistry[key] = list;
                }
                list.Add((parent, func));
            }
        }

        // For each registry, group by parent and output
        foreach (var item in byRegistry)
        {
            var (registryType, items) = (item.Key, item.Value);

            // Extract registry info from full type name (e.g., "global::Sparkitect.Stateless.TransitionRegistry")
            var registryTypeName = registryType;
            if (registryTypeName.StartsWith("global::"))
                registryTypeName = registryTypeName.Substring(8);
            var lastDot = registryTypeName.LastIndexOf('.');
            var registryShortName = lastDot >= 0 ? registryTypeName.Substring(lastDot + 1) : registryTypeName;
            var registryNamespace = lastDot >= 0 ? registryTypeName.Substring(0, lastDot) : string.Empty;

            // Get registry key from first function
            var registryKey = items[0].func.RegistryKey;

            // Build RegistryModel for this registry
            var registryModel = new RegistryModel(
                registryShortName,
                registryKey,
                registryNamespace,
                false,
                new ImmutableValueArray<RegisterMethodModel>(),
                new ImmutableValueArray<(string Key, bool Required, bool Primary)>());

            var byParent = items.GroupBy(x => x.parent.ParentTypeName);
            foreach (var parentGroup in byParent)
            {
                var parent = parentGroup.First().parent;
                var funcs = parentGroup.Select(x => x.func).OrderBy(f => f.Identifier).ToArray();

                // Build StatelessRegistrationEntry instances
                var entries = new ImmutableValueArray<RegistrationEntry>.Builder();
                foreach (var func in funcs)
                {
                    entries.Add(new StatelessRegistrationEntry(
                        func.Identifier,
                        new ImmutableValueArray<(string fileId, string fileName)>(),
                        func.WrapperFullTypeName));
                }

                // Create RegistrationUnit
                var unit = new RegistrationUnit(
                    registryModel,
                    SourceKind.Provider,
                    "Stateless",
                    entries.ToImmutableValueArray());

                // Disambiguate hintName when multiple parent classes target the same registry
                var parentShortName = parent.ParentTypeName.Contains('.')
                    ? parent.ParentTypeName.Substring(parent.ParentTypeName.LastIndexOf('.') + 1)
                    : parent.ParentTypeName;

                // Output registration using RegistryGenerator
                if (RegistryGenerator.RenderRegistryRegistrationsUnit(unit, settings, out var regCode, out var regFile, hintPrefix: parentShortName))
                {
                    context.AddSource(regFile, regCode);
                }

                // Output ID properties using RegistryGenerator
                if (RegistryGenerator.RenderRegistryIdPropertiesUnit(unit, settings, out var idCode, out var idFile, hintPrefix: parentShortName))
                {
                    context.AddSource(idFile, idCode);
                }

                // Output scheduling
                if (RenderScheduling(parent, funcs, registryType, settings, out var schedCode, out var schedFile))
                {
                    context.AddSource(schedFile, schedCode);
                }
            }
        }
    }

    internal static bool RenderWrapper(StatelessFunctionModel func, ModBuildSettings settings, out string code, out string fileName)
    {
        // Extract parent short name from full type (for nesting the wrapper class)
        var parentFull = func.ParentTypeName;
        var lastDot = parentFull.LastIndexOf('.');
        var parentShort = lastDot >= 0 ? parentFull.Substring(lastDot + 1) : parentFull;
        var parentNs = lastDot >= 0 ? parentFull.Substring(0, lastDot) : string.Empty;
        if (parentNs.StartsWith("global::"))
            parentNs = parentNs.Substring(8);

        fileName = $"{parentShort}_{func.Identifier}_Wrapper.g.cs";

        // Build ID property path: {CategoryPascal}ID.{ModIdPascal}.{IdentifierPascal}
        var categoryPascal = StringCase.ToPascalCase(func.RegistryKey);
        var modIdPascal = StringCase.ToPascalCase(settings.ModId);
        var idPropertyPath = $"global::Sparkitect.Modding.IDs.{categoryPascal}ID.{modIdPascal}.{func.IdentifierPascal}";

        var model = new
        {
            Namespace = parentNs,
            IdExtensionsNamespace = settings.ComputeOutputNamespace("IdExtensions"),
            ParentTypeName = parentShort,
            ParentIdentificationTypeName = func.ParentIdentificationTypeName, // Use fully qualified name for cross-namespace references
            MethodName = func.MethodName,
            Identifier = func.Identifier,
            WrapperClassName = func.WrapperClassName,
            WrapperFullTypeName = func.WrapperFullTypeName,
            RegistryTypeName = func.RegistryTypeName,
            IdPropertyPath = idPropertyPath,
            Parameters = func.Parameters.Select((p, i) => new
            {
                Index = i,
                p.ParameterType,
                p.IsOptional
            }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Stateless.StatelessFunctionWrapper.liquid", model, out code);
    }

    internal static bool RenderScheduling(
        StatelessParentModel parent,
        StatelessFunctionModel[] functions,
        string registryType,
        ModBuildSettings settings,
        out string code,
        out string fileName)
    {
        var registryShort = registryType.Contains('.')
            ? registryType.Substring(registryType.LastIndexOf('.') + 1)
            : registryType;

        fileName = $"{parent.ParentTypeName}_{registryShort}Scheduling.g.cs";

        var firstFunc = functions[0];

        var model = new
        {
            Namespace = parent.ParentNamespace,
            ClassName = $"{parent.ParentTypeName}_{registryShort}Scheduling",
            Functions = functions.Select(f => new
            {
                f.SchedulingTypeName,
                f.WrapperFullTypeName,
                f.ParentTypeName,
                f.ParentIdentificationTypeName,
                SchedulingParams = f.SchedulingParams.Select(p => new
                {
                    p.AttributeTypeName,
                    p.IsNullable,
                    p.IsArray,
                    Instances = p.Instances.Select(inst => new
                    {
                        GenericArgs = inst.GenericArgs.ToArray(),
                        CtorArgs = inst.CtorArgs.ToArray()
                    }).ToArray()
                }).ToArray()
            }).ToArray()
        };

        return FluidHelper.TryRenderTemplate("Stateless.StatelessFunctionScheduling.liquid", model, out code);
    }
}
