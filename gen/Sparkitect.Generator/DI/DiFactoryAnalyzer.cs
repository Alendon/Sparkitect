using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Sparkitect.Generator.DI.Diagnostics;

namespace Sparkitect.Generator.DI;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiFactoryAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(ValidateDependency, SymbolKind.NamedType);
    }

    private void ValidateDependency(SymbolAnalysisContext context)
    {
        if(context.Symbol is not INamedTypeSymbol type) return;
        if (!IsDiType(type)) return;

        if (type.Constructors.Length > 1)
        {
            ReportOnlyOneConstructor(context, type);
        }
        
        //as only a singular constructor is allowed, just validate the first one
        var constructor = type.Constructors.FirstOrDefault();
        var requiredProperties = type.GetMembers().OfType<IPropertySymbol>()
            .Where(x => x.SetMethod is not null && x.IsRequired);
        
        
    }

    private static void ReportOnlyOneConstructor(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        
    }

    private static void ReportOnlyAbstractDependencies(SymbolAnalysisContext context, INamedTypeSymbol type,
        IPropertySymbol property)
    {
        
    }
    
    private static void ReportOnlyAbstractDependencies(SymbolAnalysisContext context, INamedTypeSymbol type,
        IParameterSymbol property)
    {
        
    }

    private static bool IsDiType(INamedTypeSymbol type)
    {
        var attributes = type.GetAttributes();
        return attributes.Any(x => DiFactoryGenerator.FindFactoryBase(x) is not null);
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        OnlyAbstractDependencies,
        OnlyOneConstructor
    ];
}