using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.DI.Diagnostics;

namespace Sparkitect.Generator.DI;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiFactoryAnalyzer : DiagnosticAnalyzer
{
    const string KeyAttribute = "Sparkitect.DI.GeneratorAttributes.KeyAttribute";
    const string KeyPropertyAttribute = "Sparkitect.DI.GeneratorAttributes.KeyPropertyAttribute";
    const string FactoryTypeAttribute = "Sparkitect.DI.GeneratorAttributes.FactoryGenerationTypeAttribute";

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateDiFactory, SymbolKind.NamedType);
    }

    private void ValidateDiFactory(SymbolAnalysisContext context)
    {
        if(context.Symbol is not INamedTypeSymbol type) return;
        
        var factoryAttributes = type.GetAttributes()
            .Where(x => DiFactoryGenerator.FindFactoryBase(x) is not null)
            .ToList();
            
        if (!factoryAttributes.Any()) return;

        // Validate single/non-conflicting generation markers
        ValidateGenerationMarkers(context, type, factoryAttributes);

        // Validate only one constructor
        if (type.Constructors.Length > 1)
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
        
        // TODO Implement
        // Validate KeyedFactory specific rules
        // ValidateKeyedFactoryRules(params);
    }

    private static void ValidateGenerationMarkers(SymbolAnalysisContext context, INamedTypeSymbol type, 
        IList<AttributeData> factoryAttributes)
    {
        if (factoryAttributes.Count <= 1) return;
        
        // Check if they're conflicting (different types)
        var distinctTypes = factoryAttributes
            .Select(attr => attr.AttributeClass?.Name)
            .Distinct()
            .ToList();
                
        if (distinctTypes.Count > 1)
        {
            var typesString = string.Join(", ", distinctTypes);
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
    
    private static bool IsAbstractOrInterface(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface || type.IsAbstract;
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        OnlyAbstractDependencies,
        OnlyOneConstructor,
        RequiredPropertiesInitOnly,
        SingleGenerationMarker,
        ConflictingGenerationMarker,
        KeyedFactoryRequiresKey,
        KeyedFactoryKeyMustBeString,
        KeyedFactoryInvalidKeyProperty,
        ConflictingKeyAttributes
    ];
}