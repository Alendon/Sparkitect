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
    private const string FacadeMarkerAttributeBase = "Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all interfaces that have at least one FacadeMarker-derived attribute
        var interfacesWithFacadesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is InterfaceDeclarationSyntax,
            transform: (syntaxContext, _) =>
            {
                if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol interfaceSymbol)
                    return null;

                return ExtractFacadeMappings(interfaceSymbol);
            }).Where(m => m is not null && m.FacadeMappings.Count > 0)!;

        // Collect all interface mappings and generate entrypoint per assembly
        var allMappingsProvider = interfacesWithFacadesProvider.Collect();

        context.RegisterSourceOutput(allMappingsProvider, (context, interfaceMappings) =>
        {
            if (interfaceMappings.IsEmpty)
                return;

            // Group mappings and generate configurator
            var allFacadeMappings = interfaceMappings
                .SelectMany(im => im.FacadeMappings.Select(fm =>
                    new FacadeMapping(fm.FacadeType, im.ServiceInterfaceType)))
                .ToImmutableArray();

            if (allFacadeMappings.Length == 0)
                return;

            if (RenderFacadeConfigurator(allFacadeMappings, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });
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
                facadeMappings.Add(new FacadeTypeMapping(
                    facadeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
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

        // Check if the attribute inherits from FacadeMarkerAttribute<T>
        var currentType = attribute.AttributeClass.BaseType;
        while (currentType != null)
        {
            var originalDefinition = currentType.OriginalDefinition?.ToDisplayString(DisplayFormats.NamespaceAndType);
            if (originalDefinition == FacadeMarkerAttributeBase)
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    internal static bool RenderFacadeConfigurator(ImmutableArray<FacadeMapping> mappings, out string code, out string fileName)
    {
        fileName = "FacadeConfigurator.g.cs";

        // Sort mappings for determinism
        var sortedMappings = mappings
            .OrderBy(m => m.ServiceInterfaceType)
            .ThenBy(m => m.FacadeType)
            .ToArray();

        var model = new FacadeConfiguratorModel(
            "Sparkitect.CompilerGenerated.DI",
            "GeneratedFacadeConfigurator",
            sortedMappings.ToImmutableValueArray());

        return FluidHelper.TryRenderTemplate("DI.FacadeConfigurator.liquid", model, out code);
    }
}

/// <summary>
/// Represents all facade mappings for a single service interface
/// </summary>
internal record InterfaceFacadeMapping(
    string ServiceInterfaceType,
    List<FacadeTypeMapping> FacadeMappings);

/// <summary>
/// Represents a single facade type
/// </summary>
internal record FacadeTypeMapping(string FacadeType);

/// <summary>
/// Represents a single facade-to-service mapping
/// </summary>
public record FacadeMapping(
    string FacadeType,
    string ServiceInterfaceType);

/// <summary>
/// Model for generating the FacadeConfigurator entrypoint
/// </summary>
public record FacadeConfiguratorModel(
    string Namespace,
    string ClassName,
    ImmutableValueArray<FacadeMapping> Mappings);
