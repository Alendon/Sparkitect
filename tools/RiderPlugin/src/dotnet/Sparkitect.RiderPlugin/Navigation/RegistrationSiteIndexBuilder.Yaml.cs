using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// YAML half of the reverse-index builder: walks every <c>.sparkres.yaml</c> resource file and appends
/// each entry-ID scalar's range to the shared index under the same <see cref="RegistrationKey" /> a C#
/// attribute for that registration would produce, so reverse navigation surfaces the YAML entry.
/// </summary>
public sealed partial class RegistrationSiteIndexBuilder
{
    private const string SparkresSuffix = ".sparkres.yaml";

    partial void IndexYamlSites()
    {
        foreach (var sourceFile in EnumerateYamlSourceFiles())
            IndexYamlFile(sourceFile);
    }

    private System.Collections.Generic.IEnumerable<IPsiSourceFile> EnumerateYamlSourceFiles()
    {
        foreach (var project in _solution.GetAllProjects())
        foreach (var projectFile in project.GetAllProjectFiles())
        {
            var sourceFile = projectFile.ToSourceFile();
            if (sourceFile != null
                && sourceFile.Name.EndsWith(SparkresSuffix)
                && sourceFile.PrimaryPsiLanguage.Is<YamlLanguage>())
                yield return sourceFile;
        }
    }

    private void IndexYamlFile(IPsiSourceFile sourceFile)
    {
        if (sourceFile.GetPsiFiles<YamlLanguage>().OfType<IYamlFile>().FirstOrDefault() is not { } file)
            return;

        foreach (var scalar in file.Descendants<IPlainScalarNode>().Collect())
        {
            if (!SparkresYamlAnchors.TryGetEntryIdAnchor(scalar, out var registryFqn))
                continue;

            var entryId = SparkresYamlAnchors.GetScalarText(scalar);
            if (string.IsNullOrEmpty(entryId))
                continue;

            var modPrefix = SparkresYamlAnchors.GetModPrefix(scalar);
            if (string.IsNullOrEmpty(modPrefix))
                continue;

            var key = RegistrationKey.FromYaml(modPrefix!, registryFqn!, entryId!);
            if (key == null)
                continue;

            var range = scalar.GetDocumentRange();
            if (!range.IsValid())
                continue;

            _index.AddYamlEntries(key.Value, new LocalList<DocumentRange>(new[] { range }));
        }
    }
}
