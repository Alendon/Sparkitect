using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Generator.DI;
using static Sparkitect.Generator.DI.DiUtils;

namespace Sparkitect.Generator.GameState;

[Generator]
public class StateServiceFactoryGenerator : IIncrementalGenerator
{
    private const string StateServiceAttributeMetadataName = "Sparkitect.GameState.StateServiceAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes marked with [StateService<T>]
        var stateServiceProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            StateServiceAttributeMetadataName,
            (node, _) => node is ClassDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not INamedTypeSymbol classSymbol)
                    return null;

                return ExtractServiceFactoryModelData(classSymbol);
            }).NotNull();

        context.RegisterSourceOutput(stateServiceProvider, (context, model) =>
        {
            if (RenderServiceFactory(model, out var code, out var fileName))
            {
                context.AddSource(fileName, code);
            }
        });
    }

    internal static ServiceFactoryModel? ExtractServiceFactoryModelData(INamedTypeSymbol classSymbol)
    {
        var factoryAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(x => FindFactoryMarker(x) is not null);

        if (factoryAttribute is null)
            return null;

        var factoryMarker = FindFactoryMarker(factoryAttribute);
        var serviceType = factoryMarker?.TypeArguments.FirstOrDefault();
        var constructor = classSymbol.Constructors.FirstOrDefault();

        if (serviceType is null || constructor is null)
            return null;

        var requiredProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.SetMethod is not null)
            .Where(x => x.IsRequired);

        return new ServiceFactoryModel(
            serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            constructor.Parameters
                .Select(x => new ConstructorArgument(
                    x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToImmutableValueArray(),
            requiredProperties
                .Select(x => new RequiredProperty(
                    x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    x.SetMethod!.Name,
                    x.NullableAnnotation == NullableAnnotation.Annotated))
                .ToImmutableValueArray()
        );
    }

    internal static bool RenderServiceFactory(ServiceFactoryModel model, out string code, out string fileName)
    {
        fileName = $"{model.ImplementationTypeName}_Factory.g.cs";
        return FluidHelper.TryRenderTemplate("DI.SingletonFactory.liquid", model, out code);
    }
}
