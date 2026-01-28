using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    private const string RegisterMarkerInterface = "Sparkitect.Modding.IRegisterMarker";

    internal readonly record struct ProviderFileArg(string PropertyName, string FileName);

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
        ImmutableValueArray<(string paramType, bool isNullable)> DiParameters);

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
        string containerFullName;
        string methodOrTypeName;
        var diParamsBuilder = new ImmutableValueArray<(string paramType, bool isNullable)>.Builder();

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
        }
        else if (targetSymbol is IPropertySymbol ps)
        {
            containerFullName = ps.ContainingType.ToDisplayString(DisplayFormats.NamespaceAndType);
            methodOrTypeName = ps.Name;
        }
        else if (targetSymbol is INamedTypeSymbol nts)
        {
            containerFullName = nts.ContainingNamespace?.ToDisplayString() is { Length: > 0 } ns
                ? ns
                : string.Empty;
            methodOrTypeName = nts.ToDisplayString(DisplayFormats.NamespaceAndType);
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
            diParamsBuilder.ToImmutableValueArray());
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

        RegistrationEntry entry;
        if (cand.IsPropertyProvider)
        {
            var providerFull = $"global::{cand.ProviderContainingTypeFullName}.{cand.ProviderMethodOrTypeName}";
            entry = new PropertyRegistrationEntry(cand.Id, files, cand.MethodName, providerFull);
        }
        else if (cand.IsTypeProvider)
        {
            var typeFull = cand.ProviderMethodOrTypeName.StartsWith("global::")
                ? cand.ProviderMethodOrTypeName
                : $"global::{cand.ProviderMethodOrTypeName}";
            entry = new TypeRegistrationEntry(cand.Id, files, cand.MethodName, typeFull);
        }
        else
        {
            var providerFull = $"global::{cand.ProviderContainingTypeFullName}.{cand.ProviderMethodOrTypeName}";
            entry = new MethodRegistrationEntry(cand.Id, files, cand.MethodName, providerFull, cand.DiParameters);
        }

        var entries = new ImmutableValueArray<RegistrationEntry>.Builder();
        entries.Add(entry);

        return new RegistrationUnit(
            model,
            SourceKind.Provider,
            "Providers",
            entries.ToImmutableValueArray());
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
}
