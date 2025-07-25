using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.DI.Diagnostics;
using static Sparkitect.Generator.DI.DiUtils;

namespace Sparkitect.Generator.DI;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiFactoryAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateDiFactory, SymbolKind.NamedType);
    }

    private void ValidateDiFactory(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type) return;

        var factoryAttributes = type.GetAttributes()
            .Where(x => FindFactoryMarker(x) is not null)
            .ToList();

        if (!factoryAttributes.Any()) return;

        // Validate single/non-conflicting generation markers
        ValidateGenerationMarkers(context, type, factoryAttributes);

        // Validate only one constructor
        if (type.Constructors.Where(c => !c.IsStatic).Count() > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                OnlyOneConstructor,
                type.Locations.FirstOrDefault(),
                type.Name));
        }

        // Get the constructor (there should be only one)
        var constructor = type.Constructors.FirstOrDefault();
        if (constructor is not null)
        {
            // Validate constructor dependencies
            foreach (var parameter in constructor.Parameters)
            {
                ValidateParameterDependency(context, type, parameter);
            }
        }

        // Get required properties
        var requiredProperties = type.GetMembers().OfType<IPropertySymbol>()
            .Where(x => x.IsRequired)
            .ToList();

        // Validate required properties
        foreach (var property in requiredProperties)
        {
            // Check if property is init-only
            ValidateRequiredPropertyInitOnly(context, property);

            // Check if dependency is abstract/interface
            ValidatePropertyDependency(context, type, property);
        }

        // Validate KeyedFactory specific rules
        ValidateKeyedFactoryRules(context, type, factoryAttributes.FirstOrDefault());
    }


    private static void ValidateGenerationMarkers(SymbolAnalysisContext context, INamedTypeSymbol type,
        IList<AttributeData> factoryAttributes)
    {
        //TODO this function does not really covering what it should do
        //Currently it checks just if more than 1 factory attribute is set
        //And creates a warning if non conflicting and error when conflicting
        //In future it should just check if a single factory attribute is set
        //And validate this one (check if a generation marker is attached)

        if (factoryAttributes.Count <= 1) return;

        // Check if they're conflicting (different generation types)
        var generationTypes = factoryAttributes
            .Select(x => GetFactoryGenerationType(x.AttributeClass))
            .Distinct()
            .ToArray();

        if (generationTypes.Length > 1)
        {
            var typesString = string.Join(", ", generationTypes);
            context.ReportDiagnostic(Diagnostic.Create(
                ConflictingGenerationMarker,
                type.Locations.FirstOrDefault(),
                type.Name,
                typesString));
        }
        else
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SingleGenerationMarker,
                type.Locations.FirstOrDefault(),
                type.Name));
        }
    }

    private static void ValidateParameterDependency(SymbolAnalysisContext context, INamedTypeSymbol containingType,
        IParameterSymbol parameter)
    {
        if (parameter.Type is not INamedTypeSymbol paramType) return;
        if (IsAbstractOrInterface(paramType)) return;

        context.ReportDiagnostic(Diagnostic.Create(
            OnlyAbstractDependencies,
            parameter.Locations.FirstOrDefault(),
            parameter.Name,
            paramType.ToDisplayString()));
    }

    private static void ValidatePropertyDependency(SymbolAnalysisContext context, INamedTypeSymbol containingType,
        IPropertySymbol property)
    {
        if (property.Type is not INamedTypeSymbol propType) return;
        if (IsAbstractOrInterface(propType)) return;

        context.ReportDiagnostic(Diagnostic.Create(
            OnlyAbstractDependencies,
            property.Locations.FirstOrDefault(),
            property.Name,
            propType.ToDisplayString()));
    }

    private static void ValidateRequiredPropertyInitOnly(SymbolAnalysisContext context, IPropertySymbol property)
    {
        if (property.SetMethod is not null && !property.SetMethod.IsInitOnly)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RequiredPropertiesInitOnly,
                property.Locations.FirstOrDefault(),
                property.Name));
        }
    }

    private void ValidateKeyedFactoryRules(SymbolAnalysisContext context, INamedTypeSymbol type,
        AttributeData? factoryAttribute)
    {
        if (factoryAttribute?.AttributeConstructor is null) return;

        var generationType = GetFactoryGenerationType(factoryAttribute.AttributeClass);
        if (generationType != FactoryType.Factory) return;

        // Extract key information from named arguments only
        string? keyValue = null;
        string? keyPropertyName = null;

        foreach (var namedArg in factoryAttribute.NamedArguments)
        {
            var property = factoryAttribute.AttributeClass?.GetMembers(namedArg.Key)
                .OfType<IPropertySymbol>().FirstOrDefault();
            if (property is null) continue;

            var keyType = DetermineKeyType(property);
            switch (keyType)
            {
                case KeyType.Direct:
                    keyValue = namedArg.Value.Value?.ToString();
                    break;
                case KeyType.Property:
                    keyPropertyName = namedArg.Value.Value?.ToString();
                    break;
            }
        }

        // SPARK1006: Must have exactly one key association
        var hasKey = !string.IsNullOrEmpty(keyValue);
        var hasKeyProperty = !string.IsNullOrEmpty(keyPropertyName);

        if (!hasKey && !hasKeyProperty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryMissingKey,
                type.Locations.FirstOrDefault(),
                type.Name));
            return;
        }

        // SPARK1008: Cannot have both key types
        if (hasKey && hasKeyProperty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryConflictingKeys,
                type.Locations.FirstOrDefault(),
                type.Name));
            return;
        }

        // SPARK1007: Validate KeyProperty if present
        if (hasKeyProperty)
        {
            ValidateKeyProperty(context, type, keyPropertyName!);
        }
    }

    private static void ValidateKeyProperty(SymbolAnalysisContext context, INamedTypeSymbol type,
        string keyPropertyName)
    {
        var keyProperty = type.GetMembers(keyPropertyName).OfType<IPropertySymbol>().FirstOrDefault();

        if (keyProperty is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryInvalidKeyProperty,
                type.Locations.FirstOrDefault(),
                keyPropertyName,
                type.Name));
            return;
        }

        // Must be static
        if (!keyProperty.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryInvalidKeyProperty,
                keyProperty.Locations.FirstOrDefault(),
                keyPropertyName,
                type.Name));
            return;
        }

        // Must have a public getter
        if (keyProperty.GetMethod is null || keyProperty.GetMethod.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryInvalidKeyProperty,
                keyProperty.Locations.FirstOrDefault(),
                keyPropertyName,
                type.Name));
            return;
        }

        // Must return supported type: string, Identification, or OneOf<Identification, string>
        if (!IsValidKeyPropertyType(keyProperty.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                KeyedFactoryInvalidKeyProperty,
                keyProperty.Locations.FirstOrDefault(),
                keyPropertyName,
                type.Name));
        }
    }

    private static bool IsValidKeyPropertyType(ITypeSymbol type)
    {
        // string type
        if (type.SpecialType == SpecialType.System_String)
            return true;
            
        // Identification type
        if (type is INamedTypeSymbol namedType)
        {
            var typeName = namedType.ToDisplayString(DisplayFormats.NamespaceAndType);
            
            if (typeName == "Sparkitect.Modding.Identification")
                return true;
                
            // OneOf<Identification, string> type (only this order is valid)
            if (typeName == "OneOf.OneOf" && namedType.TypeArguments.Length == 2)
            {
                var arg1 = namedType.TypeArguments[0].ToDisplayString(DisplayFormats.NamespaceAndType);
                var arg2 = namedType.TypeArguments[1].SpecialType;
                
                return arg1 == "Sparkitect.Modding.Identification" && arg2 == SpecialType.System_String;
            }
        }
        
        return false;
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        OnlyAbstractDependencies,
        OnlyOneConstructor,
        RequiredPropertiesInitOnly,
        SingleGenerationMarker,
        ConflictingGenerationMarker,
        KeyedFactoryMissingKey,
        KeyedFactoryInvalidKeyProperty,
        KeyedFactoryConflictingKeys
    ];
}