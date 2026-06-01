using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Search;

/// <summary>
/// Routes Find Usages on a registration identifier (the C# <c>[Register…]</c> id-string literal, or the
/// resource scalar) to the identity it names — the generated <c>{Mod}{Category}IDs.{Entry}</c> leaf
/// property — so the dev gets the identity's usages directly instead of the manual navigate-to-leaf hop.
/// </summary>
/// <remarks>
/// Usage-redirect, not secondary-declaration. The identifier literal already carries a
/// <see cref="RegistrationIdReference" /> that resolves to the leaf, so Find Usages launched on the
/// literal targets the leaf identity by the standard reference-resolution rule — the redirect rides on
/// the existing forward reference, with no PSI secondary-AST machinery. This factory keeps the leaf's
/// search domain whole (it never narrows <c>GetDeclaredElementSearchDomain</c>), so the single search
/// composes with the YAML word-index participation in <see cref="SparkresSearcherFactory" />: the leaf's
/// C# usages and every <c>.sparkres.yaml</c> scalar that resolves to it appear together. The leaf is
/// surfaced as its own related element so a search anchored on the identity stays anchored on the leaf.
/// </remarks>
[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class IdentifierUsageSearch : DomainSpecificSearcherFactoryBase
{
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
        languageType.Is<CSharpLanguage>();

    public override IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
    {
        // The identity is the generated leaf id property. When the search is anchored on it (directly, or
        // redirected onto it from an identifier literal via RegistrationIdReference), surface it as its
        // own related element so the leaf stays the search target and its cross-surface usages compose.
        if (element is IProperty property && RegistrationKey.FromLeafProperty(property) != null)
            yield return new RelatedDeclaredElement(property);
    }
}
