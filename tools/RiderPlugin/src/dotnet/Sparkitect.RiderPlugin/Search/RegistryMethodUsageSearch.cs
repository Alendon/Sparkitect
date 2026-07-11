using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Search;

/// <summary>
/// Routes Find Usages on a <c>[RegistryMethod]</c>-decorated method to the actual per-mod registration
/// sites authored through it. Mods never call the method directly — the generated <c>Registrations&lt;&gt;</c>
/// boilerplate invokes it — so stock Find Usages lands only on the <c>.g.cs</c> dispatcher calls. This
/// factory redirects onto the registry's generated registration attribute type(s), whose usages ARE the
/// <c>[RegisterX("id")]</c> application sites, so the search surfaces the real registrations cross-mod.
/// </summary>
/// <remarks>
/// Usage-redirect, not secondary-declaration (same shape as <see cref="IdentifierUsageSearch" />). The
/// registration attribute type is nested in the registry class and carries the forward
/// <c>RegistrationMarkerAttribute</c>; returning it as a related declared element lets the platform's own
/// usage search compose the attribute-application sites into the method's results. Category resolution
/// reuses <see cref="RegistrationMarkerPredicate" /> + <see cref="RegistrationKey.MarkerCategory" /> — the
/// single source of truth — never namespace/short-name parsing. On-demand; no source-generator index.
/// </remarks>
[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class RegistryMethodUsageSearch : DomainSpecificSearcherFactoryBase
{
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
        languageType.Is<CSharpLanguage>();

    public override IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
    {
        if (element is not IMethod method)
            yield break;

        // Surface the registry's registration attribute type(s); the platform searches their usages, which
        // are the [RegisterX(...)] application sites. Gate is cheap: empty for any non-registry method.
        foreach (var attributeType in RegistryMethodRegistrations.ResolveRegistrationAttributeTypes(method))
            yield return new RelatedDeclaredElement(attributeType);
    }
}

/// <summary>
/// Shared <c>[RegistryMethod]</c> method -> category -> registration-site aggregation, consumed by both the
/// searcher redirect and the "N registrations" code-vision. The category-resolution edge lives here once:
/// method -> containing <c>[Registry(Identifier=X)]</c> class -> category X -> the registry's nested
/// registration attribute type(s) whose <c>RegistrationMarkerAttribute</c> category equals X. On-demand
/// solution-wide usage search over those attribute types; zero new PSI primitives, no generator index.
/// </summary>
internal static class RegistryMethodRegistrations
{
    private const string RegistryMethodAttributeFullName = "Sparkitect.Modding.RegistryMethodAttribute";
    private const string RegistryAttributeFullName = "Sparkitect.Modding.RegistryAttribute";
    private const string RegistryIdentifierParameter = "Identifier";

    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(RegistryMethodRegistrations));

    /// <summary>
    /// The registry's generated registration attribute type(s) for a <c>[RegistryMethod]</c> method, or an
    /// empty list when the method is not a registry method or no category/attribute type resolves. Cheap
    /// metadata reads only — the gate every solution-wide walk runs behind (T-63-04).
    /// </summary>
    public static IReadOnlyList<ITypeElement> ResolveRegistrationAttributeTypes(IMethod method)
    {
        if (!CarriesRegistryMethod(method))
            return Array.Empty<ITypeElement>();

        var registryClass = method.GetContainingType();
        if (registryClass == null)
        {
            Logger.Verbose("Registry-method aggregation: [RegistryMethod] method has no containing type.");
            return Array.Empty<ITypeElement>();
        }

        var category = ResolveCategory(registryClass);
        if (string.IsNullOrEmpty(category))
        {
            Logger.Verbose($"Registry-method aggregation: '{method.ShortName}' container has no resolvable [Registry(Identifier=…)] category.");
            return Array.Empty<ITypeElement>();
        }

        var attributeTypes = registryClass.NestedTypes
            .Where(t => RegistrationMarkerPredicate.IsRegistrationAttribute(t)
                        && RegistrationKey.MarkerCategory(t) == category)
            .ToList();

        if (attributeTypes.Count == 0)
            Logger.Verbose($"Registry-method aggregation: category '{category}' resolved no nested registration attribute type.");

        return attributeTypes;
    }

    /// <summary>
    /// Distinct declarations carrying one of the registration attribute types — the real cross-mod
    /// registration sites. Solution-wide usage search on each attribute type, filtered to actual attribute
    /// applications (drops incidental <c>typeof</c>/other references).
    /// </summary>
    public static List<IDeclaredElement> FindRegistrationSites(
        IReadOnlyList<ITypeElement> attributeTypes, ISolution solution, IPsiServices services)
    {
        var owners = new HashSet<IDeclaredElement>();
        if (attributeTypes.Count == 0)
            return owners.ToList();

        var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(solution, false);
        foreach (var attributeType in attributeTypes)
        {
            var references = services.Finder.FindReferences(attributeType, searchDomain, NullProgressIndicator.Create());
            foreach (var reference in references)
            {
                var owner = ResolveRegistrationOwner(reference.GetTreeNode());
                if (owner != null)
                    owners.Add(owner);
            }
        }

        return owners.ToList();
    }

    private static bool CarriesRegistryMethod(IMethod method)
    {
        foreach (var instance in method.GetAttributeInstances(AttributesSource.Self))
        {
            if (instance.GetAttributeType().GetTypeElement()?.GetClrName().FullName == RegistryMethodAttributeFullName)
                return true;
        }

        return false;
    }

    /// <summary>The registry's declared category — the <c>Identifier</c> of its <c>[Registry]</c> attribute — or null.</summary>
    private static string? ResolveCategory(ITypeElement registryClass)
    {
        var instances = registryClass.GetAttributeInstances(
            new ClrTypeName(RegistryAttributeFullName), AttributesSource.Self);
        foreach (var instance in instances)
        {
            try
            {
                var value = instance.NamedParameter(RegistryIdentifierParameter);
                if (value is { IsBadValue: false, IsConstant: true } && value.ConstantValue.IsString())
                    return value.ConstantValue.AsString();
            }
            catch (NullReferenceException)
            {
                // net10.0 constant-evaluator NRE (RESEARCH Pitfall 4): degrade to absent rather than throw.
                return null;
            }
        }

        return null;
    }

    /// <summary>The declaration a reference belongs to, but only when the reference sits inside an attribute application.</summary>
    private static IDeclaredElement? ResolveRegistrationOwner(ITreeNode? node)
    {
        var attribute = FindAncestor<IAttribute>(node);
        if (attribute == null)
            return null;

        return FindAncestor<IDeclaration>(attribute)?.DeclaredElement;
    }

    private static T? FindAncestor<T>(ITreeNode? node) where T : class, ITreeNode
    {
        for (var current = node; current != null; current = current.Parent)
            if (current is T match)
                return match;

        return null;
    }
}
