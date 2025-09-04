using System;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    private const string RegisterMarkerInterface = "Sparkitect.Modding.IRegisterMarker";

    internal readonly record struct ProviderFileArg(string PropertyName, string FileName);

    internal sealed record ProviderCandidate(
        string RegistryTypeName,
        string MethodName,
        string Id,
        bool IsTypeProvider,
        string ProviderContainingTypeFullName,
        string ProviderMethodOrTypeName,
        ImmutableValueArray<ProviderFileArg> Files,
        string SourcePath,
        int SourceSpanStart);

    internal static bool TryExtractProviderInfo(AttributeData attribute,
        out string registryTypeName, out string methodName, out bool isRegisterMarker)
    {
        registryTypeName = string.Empty;
        methodName = string.Empty;
        isRegisterMarker = false;

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

        // Try to derive registry type name from the containing type (nested attribute)
        if (attrClass.ContainingType is not null)
        {
            registryTypeName = attrClass.ContainingType.Name;
            return true;
        }

        // Fallback: try display string to extract nested pattern Foo.BarAttribute -> Foo
        var display = attrClass.ToDisplayString();
        var lastDot = display.LastIndexOf('.')
                      - (display.EndsWith("Attribute") ? "Attribute".Length : 0);
        if (lastDot > 0)
        {
            var before = display.Substring(0, display.LastIndexOf('.'));
            var simpleLastDot = before.LastIndexOf('.');
            registryTypeName = simpleLastDot >= 0 ? before.Substring(simpleLastDot + 1) : before;
        }

        return !string.IsNullOrEmpty(registryTypeName) && !string.IsNullOrEmpty(methodName);
    }

    private static string TrimAttributeSuffix(string name)
    {
        return name.EndsWith("Attribute") ? name.Substring(0, name.Length - "Attribute".Length) : name;
    }

    internal static ProviderCandidate? TryBuildProviderCandidate(GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.Node is not AttributeSyntax attrSyntax) return null;

        // Determine declaration target (method or class)
        var decl = attrSyntax.Parent?.Parent;
        if (decl is null) return null;

        ISymbol? targetSymbol = decl switch
        {
            MethodDeclarationSyntax mds => context.SemanticModel.GetDeclaredSymbol(mds, cancellationToken),
            ClassDeclarationSyntax cds => context.SemanticModel.GetDeclaredSymbol(cds, cancellationToken),
            _ => null
        };
        if (targetSymbol is null) return null;

        // Find the concrete AttributeData instance that corresponds to this syntax
        var attributeData = targetSymbol.GetAttributes().FirstOrDefault(a =>
            a.ApplicationSyntaxReference?.Span.Equals(attrSyntax.Span) == true);
        if (attributeData is null) return null;

        if (!TryExtractProviderInfo(attributeData, out var registryTypeName, out var methodName, out var isMarker))
            return null;

        // Extract identifier (first ctor arg)
        if (attributeData.ConstructorArguments.Length == 0 ||
            attributeData.ConstructorArguments[0].Value is not string id || string.IsNullOrWhiteSpace(id))
            return null;

        // Collect file named arguments (only provided string values)
        var filesBuilder = new ImmutableValueArray<ProviderFileArg>.Builder();
        foreach (var kvp in attributeData.NamedArguments)
        {
            var (propName, typed) = (kvp.Key, kvp.Value);
            if (typed.Value is string s && !string.IsNullOrWhiteSpace(s))
            {
                filesBuilder.Add(new ProviderFileArg(propName, s));
            }
        }

        bool isTypeProvider = targetSymbol is INamedTypeSymbol;
        string containerFullName;
        string methodOrTypeName;

        if (targetSymbol is IMethodSymbol ms)
        {
            containerFullName = ms.ContainingType.ToDisplayString(DisplayFormats.NamespaceAndType);
            methodOrTypeName = ms.Name;
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
            methodName,
            id,
            isTypeProvider,
            containerFullName,
            methodOrTypeName,
            filesBuilder.ToImmutableValueArray(),
            attrSyntax.SyntaxTree.FilePath ?? string.Empty,
            attrSyntax.Span.Start);
    }

    internal static RegistrationUnit? MapProviderCandidateToUnit(ProviderCandidate cand, RegistryMap regMap)
    {
        if (!regMap.TryGetByTypeName(cand.RegistryTypeName, out var model) || model is null)
            return null;

        var isSingleFile = model.ResourceFiles.Count == 1;
        var propToId = new Dictionary<string, string>();
        if (isSingleFile)
        {
            propToId["File"] = "default";
        }
        else
        {
            foreach (var rf in model.ResourceFiles)
            {
                propToId[RegistryGenerator.ToPascalCase(rf.identifier)] = rf.identifier;
            }
        }

        var filesBuilder = new ImmutableValueArray<(string fileId, string fileName)>.Builder();
        foreach (var f in cand.Files)
        {
            if (propToId.TryGetValue(f.PropertyName, out var id))
            {
                filesBuilder.Add((id, f.FileName));
            }
        }

        var files = filesBuilder.ToImmutableValueArray();

        var kind = cand.IsTypeProvider ? EntryKind.Type : EntryKind.Method;

        var entry = new RegistrationEntry(
            cand.Id,
            kind,
            cand.MethodName,
            cand.ProviderContainingTypeFullName,
            cand.ProviderMethodOrTypeName,
            files);

        var entries = new ImmutableValueArray<RegistrationEntry>.Builder();
        entries.Add(entry);

        return new RegistrationUnit(
            model,
            SourceKind.Provider,
            ComputeStableTag(cand.SourcePath, cand.SourceSpanStart),
            entries.ToImmutableValueArray());
    }

    internal static string ComputeStableTag(string path, int spanStart)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (path?.GetHashCode() ?? 0);
            hash = hash * 31 + spanStart.GetHashCode();
            return Math.Abs(hash).ToString("x");
        }
    }

    // Intentionally no grouping here to keep pipeline simple and testable; this returns per-candidate transformations only.
}
