using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Search;

/// <summary>
/// Makes the generated leaf id property findable inside <c>.sparkres.yaml</c> files. The leaf member is
/// Pascal-cased (<c>ClearColor</c>) while the resource scalar keeps the authored snake_case id
/// (<c>clear_color</c>), so the default word index — keyed on the member word — never opens the YAML
/// files and the scalar usage is silently missed. For a generated leaf this factory yields the empty
/// word, which switches word filtering off so every YAML file in the search domain is scanned and the
/// custom <see cref="SparkresEntryIdReference" /> on each scalar resolves back to the leaf. This is the
/// keep-it-findable direction: it never strips generator-emitted results.
/// </summary>
[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class SparkresSearcherFactory : DomainSpecificSearcherFactoryBase
{
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
        languageType.Is<YamlLanguage>();

    public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
        // Only generated leaf id properties cross into YAML. For them the authored scalar id differs in
        // case from the member word, and reconstructing it would be lossy, so disable word filtering
        // (empty word) and let the reference resolve confirm each scalar. Everything else keeps the
        // default member-word filtering.
        if (element is IProperty property && RegistrationKey.FromLeafProperty(property) != null)
            yield return string.Empty;
    }
}
