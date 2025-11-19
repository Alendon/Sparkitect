using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Sparkitect.Generator.GameState.StateUtils;

namespace Sparkitect.Generator.DI;

[Generator]
public class FacadeMappingGenerator : IIncrementalGenerator
{
    private const string FacadeMarkerName = "Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var markerAttributeProvider = context.SyntaxProvider.CreateSyntaxProvider<string?>(
            predicate: (node, _) => node is ClassDeclarationSyntax,
            transform: (syntaxContext, _) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol classSymbol)
                    return null;

                if (IsFacadeMarkerAttributeClass(classSymbol))
                {
                    return classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGenericsOptions(SymbolDisplayGenericsOptions.None));
                }

                return null;
            }).NotNull();

        var allMarkerAttributesProvider = markerAttributeProvider.Collect();

        context.RegisterSourceOutput(allMarkerAttributesProvider, (context, markerAttributeTypes) =>
        {
            foreach (var markerAttributeType in markerAttributeTypes.Distinct())
            {
                if (RenderFacadeConfiguratorInterface(markerAttributeType, out var interfaceCode, out var interfaceFileName))
                {
                    context.AddSource(interfaceFileName, interfaceCode);
                }
            }
        });

        var interfacesWithFacadesProvider = context.SyntaxProvider.CreateSyntaxProvider<InterfaceFacadeMapping?>(
            predicate: (node, _) => node is InterfaceDeclarationSyntax,
            transform: (syntaxContext, _) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol interfaceSymbol)
                    return null;

                return ExtractFacadeMappings(interfaceSymbol);
            }).NotNull();

        var allMappingsProvider = interfacesWithFacadesProvider.Collect();

        context.RegisterSourceOutput(allMappingsProvider, (context, interfaceMappings) =>
        {
            if (interfaceMappings.IsEmpty)
                return;

            // Group mappings by marker attribute type
            var mappingsByMarker = new Dictionary<string, List<FacadeMapping>>();

            foreach (var interfaceMapping in interfaceMappings)
            {
                foreach (var facadeMapping in interfaceMapping.FacadeMappings)
                {
                    var markerAttributeType = facadeMapping.MarkerAttributeType;

                    if (!mappingsByMarker.ContainsKey(markerAttributeType))
                    {
                        mappingsByMarker[markerAttributeType] = new List<FacadeMapping>();
                    }

                    mappingsByMarker[markerAttributeType].Add(
                        new FacadeMapping(facadeMapping.FacadeType, interfaceMapping.ServiceInterfaceType, markerAttributeType));
                }
            }

            // Generate configurator per marker attribute type
            foreach (var kvp in mappingsByMarker)
            {
                var markerAttributeType = kvp.Key;
                var mappings = kvp.Value;

                // Generate the configurator implementation
                if (RenderFacadeConfigurator(mappings.ToImmutableArray(), markerAttributeType, out var code, out var fileName))
                {
                    context.AddSource(fileName, code);
                }
            }
        });
    }

    private static bool IsFacadeMarkerAttributeClass(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.TypeKind != TypeKind.Class)
            return false;

        var currentType = classSymbol.BaseType;
        while (currentType != null)
        {
            var originalDefinition = currentType.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType);
            if (originalDefinition == FacadeMarkerName)
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    internal static InterfaceFacadeMapping? ExtractFacadeMappings(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol.TypeKind != TypeKind.Interface)
            return null;

        var facadeMappings = new List<FacadeTypeMapping>();

        // Scan all attributes on the interface
        foreach (var attr in interfaceSymbol.GetAttributes())
        {
            // Check if attribute inherits from FacadeMarkerAttribute<T>
            if (!IsFacadeMarkerAttribute(attr))
                continue;

            // Extract the facade type from the attribute's type argument
            if (attr.AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol facadeType)
            {
                var markerAttributeType = attr.AttributeClass.ConstructedFrom.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGenericsOptions(SymbolDisplayGenericsOptions.None));

                facadeMappings.Add(new FacadeTypeMapping(
                    facadeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    markerAttributeType));
            }
        }

        if (facadeMappings.Count == 0)
            return null;

        return new InterfaceFacadeMapping(
            interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            facadeMappings);
    }

    private static bool IsFacadeMarkerAttribute(AttributeData attribute)
    {
        if (attribute.AttributeClass is null)
            return false;

        var currentType = attribute.AttributeClass.BaseType;
        while (currentType != null)
        {
            var originalDefinition = currentType.OriginalDefinition.ToDisplayString(DisplayFormats.NamespaceAndType);
            if (originalDefinition == FacadeMarkerName)
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    internal static bool RenderFacadeConfiguratorInterface(string markerAttributeType, out string code, out string fileName)
    {
        var configuratorInterfaceName = GetConfiguratorInterfaceName(markerAttributeType);
        var markerNamespace = ExtractNamespace(markerAttributeType);
        fileName = $"{configuratorInterfaceName}.g.cs";

        var model = new FacadeConfiguratorInterfaceModel(
            markerNamespace,
            configuratorInterfaceName,
            markerAttributeType);

        return FluidHelper.TryRenderTemplate("DI.FacadeConfiguratorInterface.liquid", model, out code);
    }

    internal static bool RenderFacadeConfigurator(ImmutableArray<FacadeMapping> mappings, string markerAttributeType, out string code, out string fileName)
    {
        var configuratorInterfaceName = GetConfiguratorInterfaceName(markerAttributeType);
        var configuratorSimpleName = configuratorInterfaceName.Substring(1);
        var markerNamespace = ExtractNamespace(markerAttributeType);
        fileName = $"{configuratorSimpleName}.g.cs";

        var sortedMappings = mappings
            .OrderBy(m => m.ServiceInterfaceType)
            .ThenBy(m => m.FacadeType)
            .ToArray();

        var model = new FacadeConfiguratorModel(
            $"{markerNamespace}.CompilerGenerated.DI",
            $"Generated{configuratorSimpleName}",
            markerAttributeType,
            $"global::{markerNamespace}.{configuratorInterfaceName}",
            sortedMappings.ToImmutableValueArray());

        return FluidHelper.TryRenderTemplate("DI.FacadeConfigurator.liquid", model, out code);
    }

    private static string GetConfiguratorInterfaceName(string markerAttributeType)
    {
        var markerSimpleName = markerAttributeType.Split('.').Last();
        if (markerSimpleName.EndsWith("Attribute"))
        {
            markerSimpleName = markerSimpleName.Substring(0, markerSimpleName.Length - "Attribute".Length);
        }

        return $"I{markerSimpleName}Configurator";
    }

    private static string ExtractNamespace(string fullyQualifiedType)
    {
        var typeWithoutGlobal = fullyQualifiedType.StartsWith("global::")
            ? fullyQualifiedType.Substring("global::".Length)
            : fullyQualifiedType;

        var lastDotIndex = typeWithoutGlobal.LastIndexOf('.');
        return lastDotIndex > 0 ? typeWithoutGlobal.Substring(0, lastDotIndex) : string.Empty;
    }
}

/// <summary>
/// Represents all facade mappings for a single service interface
/// </summary>
internal record InterfaceFacadeMapping(
    string ServiceInterfaceType,
    List<FacadeTypeMapping> FacadeMappings);

/// <summary>
/// Represents a single facade type with its marker attribute
/// </summary>
internal record FacadeTypeMapping(
    string FacadeType,
    string MarkerAttributeType);

/// <summary>
/// Represents a single facade-to-service mapping
/// </summary>
public record FacadeMapping(
    string FacadeType,
    string ServiceInterfaceType,
    string MarkerAttributeType);

/// <summary>
/// Model for generating the FacadeConfiguratorInterface
/// </summary>
public record FacadeConfiguratorInterfaceModel(
    string Namespace,
    string InterfaceName,
    string MarkerAttributeType);

/// <summary>
/// Model for generating the FacadeConfigurator entrypoint
/// </summary>
public record FacadeConfiguratorModel(
    string Namespace,
    string ClassName,
    string MarkerAttributeType,
    string InterfaceName,
    ImmutableValueArray<FacadeMapping> Mappings);
