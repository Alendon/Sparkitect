using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.DI.Pipeline;

/// <summary>
/// Static toolbox for DI code generation pipeline.
/// Provides extraction, rendering, and registration functions consumed by source generators.
/// </summary>
public static class DiPipeline
{
    private const string OptionalModDependentFullName =
        "Sparkitect.Modding.OptionalModDependentAttribute";

    /// <summary>
    /// Extracts a <see cref="FactoryModel"/> from a named type symbol.
    /// The caller provides the intent (Service or Keyed) and base type;
    /// this method extracts constructor args, required properties, and optional mod IDs.
    /// </summary>
    /// <returns>A <see cref="FactoryModel"/> or null if extraction fails.</returns>
    public static FactoryModel? ExtractFactory(INamedTypeSymbol symbol, FactoryIntent intent, string baseType)
    {
        var constructor = symbol.Constructors.FirstOrDefault();
        if (constructor is null) return null;

        var requiredProperties = GetInjectableProperties(symbol);

        var optionalModIds = ExtractConditionalModIds(symbol);

        return new FactoryModel(
            baseType,
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            constructor.Parameters
                .Select(x =>
                    new ConstructorArgument(
                        x.Type.ToDisplayString(DisplayFormats.NamespaceAndType.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters)),
                        x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToImmutableValueArray(),
            requiredProperties.Select(x =>
                    new RequiredProperty(
                        x.Type.ToDisplayString(DisplayFormats.NamespaceAndType.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters)),
                        x.SetMethod!.Name,
                        x.NullableAnnotation == NullableAnnotation.Annotated,
                        x.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                .ToImmutableValueArray(),
            intent,
            optionalModIds);
    }

    /// <summary>
    /// Extracts conditional mod IDs from all [OptionalModDependent("mod_id")] attributes on the symbol.
    /// </summary>
    public static ImmutableValueArray<string> ExtractConditionalModIds(INamedTypeSymbol symbol)
    {
        var modIds = new ImmutableValueArray<string>.Builder();

        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(DisplayFormats.NamespaceAndType) != OptionalModDependentFullName)
                continue;

            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string modId)
            {
                modIds.Add(modId);
            }
        }

        return modIds.ToImmutableValueArray();
    }

    /// <summary>
    /// Renders a factory class from a <see cref="FactoryModel"/>.
    /// Dispatches to the appropriate template based on the model's intent.
    /// </summary>
    public static bool RenderFactory(FactoryModel model, out string code, out string fileName)
    {
        switch (model.Intent)
        {
            case FactoryIntent.Service:
                fileName = $"{model.ImplementationTypeName}_Factory.g.cs";
                return FluidHelper.TryRenderTemplate("DI.ServiceFactory.liquid", model, out code);

            case FactoryIntent.Keyed:
                fileName = $"{model.ImplementationTypeName}_KeyedFactory.g.cs";
                return FluidHelper.TryRenderTemplate("DI.KeyedFactory.liquid", model, out code);

            default:
                code = string.Empty;
                fileName = string.Empty;
                return false;
        }
    }

    /// <summary>
    /// Creates a <see cref="RegistrationModel"/> from a factory model and its symbol.
    /// Extracts conditional mod IDs from the symbol's attributes.
    /// </summary>
    public static RegistrationModel ToRegistration(FactoryModel factory, INamedTypeSymbol symbol)
    {
        var conditionalModIds = ExtractConditionalModIds(symbol);

        var factoryTypeName = factory.Intent switch
        {
            FactoryIntent.Service =>
                $"global::{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_Factory",
            FactoryIntent.Keyed =>
                $"global::{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_KeyedFactory",
            _ => $"global::{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_Factory"
        };

        return new RegistrationModel(factoryTypeName, conditionalModIds);
    }

    /// <summary>
    /// Renders a configurator class from registrations and options.
    /// Handles both complete and partial configurators with conditional registration guard methods.
    /// </summary>
    public static bool RenderConfigurator(
        ImmutableValueArray<RegistrationModel> registrations,
        ConfiguratorOptions options,
        out string code,
        out string fileName)
    {
        fileName = $"{options.ClassName}.g.cs";

        var methodVisibility = options.IsPartial ? "private" : "public";
        var methodName = options.MethodName ?? "Configure";

        var builderType = options.Kind switch
        {
            ConfiguratorKind.Service =>
                "global::Sparkitect.DI.Container.ICoreContainerBuilder",
            ConfiguratorKind.Keyed keyed =>
                $"global::Sparkitect.DI.Container.IFactoryContainerBuilder<global::{keyed.BaseType}>",
            _ => "global::Sparkitect.DI.Container.ICoreContainerBuilder"
        };

        var unconditionalRegistrations = new List<object>();
        var conditionalRegistrations = new List<object>();

        foreach (var reg in registrations)
        {
            if (reg.ConditionalModIds.Count == 0)
            {
                var registrationCode = options.Kind switch
                {
                    ConfiguratorKind.Service => $"builder.Register<{reg.FactoryTypeName}>();",
                    ConfiguratorKind.Keyed => $"builder.Register(new {reg.FactoryTypeName}());",
                    _ => $"builder.Register<{reg.FactoryTypeName}>();"
                };

                unconditionalRegistrations.Add(new { RegistrationCode = registrationCode });
            }
            else
            {
                var condition = string.Join(" && ",
                    reg.ConditionalModIds.Select(modId => $"loadedMods.Contains(\"{modId}\")"));

                var sanitizedName = SanitizeForMethodName(reg.FactoryTypeName);
                var guardMethodName = $"Register_{sanitizedName}";

                var registrationCode = options.Kind switch
                {
                    ConfiguratorKind.Service => $"builder.Register<{reg.FactoryTypeName}>();",
                    ConfiguratorKind.Keyed => $"builder.Register(new {reg.FactoryTypeName}());",
                    _ => $"builder.Register<{reg.FactoryTypeName}>();"
                };

                conditionalRegistrations.Add(new
                {
                    Condition = condition,
                    GuardMethodName = guardMethodName,
                    ModIds = reg.ConditionalModIds.ToArray(),
                    RegistrationCode = registrationCode
                });
            }
        }

        var templateModel = new
        {
            options.Namespace,
            options.ClassName,
            options.IsPartial,
            options.EntrypointAttribute,
            options.BaseType,
            options.ModuleTypeFullName,
            MethodVisibility = methodVisibility,
            MethodName = methodName,
            BuilderType = builderType,
            UnconditionalRegistrations = unconditionalRegistrations.ToArray(),
            ConditionalRegistrations = conditionalRegistrations.ToArray()
        };

        return FluidHelper.TryRenderTemplate("DI.Configurator.liquid", templateModel, out code);
    }

    private const string FacadeMarkerBaseName =
        "Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute";

    /// <summary>
    /// Extracts facade metadata from a symbol's constructor parameters and injectable properties.
    /// For each dependency type that is an interface with a facade marker attribute matching
    /// the given category, produces a <see cref="FacadeMetadataModel"/>.
    /// </summary>
    /// <param name="symbol">The class symbol whose dependencies to scan.</param>
    /// <param name="facadeCategoryTypeName">
    /// The full type name of the facade category attribute (e.g., "Sparkitect.GameState.StateFacadeAttribute").
    /// </param>
    /// <returns>A list of facade metadata models for matching dependencies.</returns>
    public static IReadOnlyList<FacadeMetadataModel> ExtractFacadeMetadata(
        INamedTypeSymbol symbol, string facadeCategoryTypeName)
    {
        var results = new List<FacadeMetadataModel>();

        // Scan constructor parameters
        var constructor = symbol.Constructors.FirstOrDefault();
        if (constructor is not null)
        {
            foreach (var param in constructor.Parameters)
            {
                if (param.Type is INamedTypeSymbol paramType && paramType.TypeKind == TypeKind.Interface)
                {
                    CollectFacadeMappings(paramType, facadeCategoryTypeName, results);
                }
            }
        }

        // Scan injectable properties
        var properties = GetInjectableProperties(symbol);
        foreach (var prop in properties)
        {
            if (prop.Type is INamedTypeSymbol propType && propType.TypeKind == TypeKind.Interface)
            {
                CollectFacadeMappings(propType, facadeCategoryTypeName, results);
            }
        }

        return results;
    }

    /// <summary>
    /// Renders a metadata entrypoint class from the given wrapper type and metadata models.
    /// Uses the MetadataEntrypoint.liquid template for the shell class.
    /// </summary>
    /// <param name="wrapperTypeName">The fully qualified wrapper type name (used for the attribute and interface generic arg).</param>
    /// <param name="wrapperTypeNamespace">The namespace of the wrapper type (used for className derivation).</param>
    /// <param name="models">The metadata models to render as inner code blocks.</param>
    /// <param name="settings">Build settings for namespace computation.</param>
    /// <param name="code">The rendered source code output.</param>
    /// <param name="fileName">The generated file name.</param>
    /// <returns>True if rendering succeeded.</returns>
    public static bool RenderMetadataEntrypoint(
        string wrapperTypeName,
        string wrapperTypeNamespace,
        IReadOnlyList<IMetadataModel> models,
        ModBuildSettings settings,
        out string code,
        out string fileName)
    {
        if (models.Count == 0)
        {
            code = string.Empty;
            fileName = string.Empty;
            return false;
        }

        var ns = string.IsNullOrWhiteSpace(settings.SgOutputNamespace)
            ? $"{wrapperTypeNamespace}.CompilerGenerated.DI"
            : $"{settings.SgOutputNamespace}.CompilerGenerated.DI";

        // Build inner code blocks from each model's RenderCodeLines()
        var innerCodeBlocks = new List<string>();
        foreach (var model in models)
        {
            var lines = model.RenderCodeLines();
            var block = string.Join("\n", lines.Select(l => $"        {l}"));
            innerCodeBlocks.Add(block);
        }

        // Derive a safe class name from the wrapper type name
        var safeClassName = wrapperTypeName.Replace(".", "_").Replace("+", "_");
        var className = $"{safeClassName}_ResolutionMetadata";

        fileName = $"{className}.g.cs";

        var templateModel = new
        {
            Namespace = ns,
            WrapperTypeName = wrapperTypeName,
            ClassName = className,
            InnerCodeBlocks = innerCodeBlocks.ToArray()
        };

        return FluidHelper.TryRenderTemplate("DI.MetadataEntrypoint.liquid", templateModel, out code);
    }

    /// <summary>
    /// Scans an interface symbol's attributes for facade markers matching the given category.
    /// </summary>
    private static void CollectFacadeMappings(
        INamedTypeSymbol interfaceSymbol, string facadeCategoryTypeName, List<FacadeMetadataModel> results)
    {
        foreach (var attr in interfaceSymbol.GetAttributes())
        {
            if (attr.AttributeClass is null)
                continue;

            // Check if this attribute inherits from FacadeMarkerAttribute<T>
            if (!IsFacadeMarkerAttribute(attr.AttributeClass))
                continue;

            // Check if the attribute's ConstructedFrom matches the category
            var constructedFrom = attr.AttributeClass.ConstructedFrom.ToDisplayString(
                DisplayFormats.NamespaceAndType);

            if (constructedFrom != facadeCategoryTypeName)
                continue;

            // Extract the facade type from the attribute's type argument
            if (attr.AttributeClass.TypeArguments.FirstOrDefault() is INamedTypeSymbol facadeType)
            {
                var dependencyType = interfaceSymbol.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);
                var facadedType = facadeType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);

                results.Add(new FacadeMetadataModel(dependencyType, facadedType));
            }
        }
    }

    /// <summary>
    /// Checks if a type symbol inherits from FacadeMarkerAttribute (the generic base).
    /// </summary>
    private static bool IsFacadeMarkerAttribute(INamedTypeSymbol attributeClass)
    {
        var currentType = attributeClass.BaseType;
        while (currentType is not null)
        {
            var originalDefinition = currentType.OriginalDefinition.ToDisplayString(
                DisplayFormats.NamespaceAndType);
            if (originalDefinition == FacadeMarkerBaseName)
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Walks the type hierarchy collecting required properties with a set method.
    /// Walks injectable (required + has setter) properties for factory code generation.
    /// </summary>
    private static IEnumerable<IPropertySymbol> GetInjectableProperties(INamedTypeSymbol typeSymbol)
    {
        List<INamedTypeSymbol> stack = [];
        var walker = typeSymbol;
        while (walker is not null)
        {
            stack.Add(walker);
            walker = walker.BaseType;
        }

        var properties = new HashSet<IPropertySymbol>(
            stack.SelectMany(x => x.GetMembers().OfType<IPropertySymbol>()),
            SymbolEqualityComparer.Default);
        return properties.Where(x => x.SetMethod is not null && x.IsRequired);
    }

    /// <summary>
    /// Sanitizes a fully qualified type name for use as a method name suffix.
    /// Replaces non-identifier characters with underscores.
    /// </summary>
    private static string SanitizeForMethodName(string typeName)
    {
        // Remove global:: prefix and replace non-alphanumeric characters
        var sanitized = typeName.Replace("global::", "");
        var sb = new StringBuilder(sanitized.Length);
        foreach (var c in sanitized)
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }
        return sb.ToString();
    }
}
