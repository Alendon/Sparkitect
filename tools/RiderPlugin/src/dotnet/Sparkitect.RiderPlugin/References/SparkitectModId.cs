using System.Linq;
using System.Xml.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Edit-time resolver for a mod's <c>&lt;ModId&gt;</c> MSBuild property — the single reliable source
/// for the <c>{Mod}</c> segment of the generated <c>{Mod}{Category}IDs</c> struct
/// (<c>StringCase.ToPascalCase(ModId)</c> in the generator). Replaces the brittle project-name guess:
/// PongMod's mod-id is <c>pong_mod</c>, not the project name "PongMod".
/// </summary>
public static class SparkitectModId
{
    private const string ModIdElementName = "ModId";

    /// <summary>The raw <c>&lt;ModId&gt;</c> value for the project owning <paramref name="typeElement" />, or null.</summary>
    public static string? Resolve(ITypeElement typeElement)
    {
        var module = (typeElement as IClrDeclaredElement)?.Module;
        return Resolve(GetProject(module));
    }

    /// <summary>The raw <c>&lt;ModId&gt;</c> value for the project owning <paramref name="node" />, or null.</summary>
    public static string? Resolve(ITreeNode node) => Resolve(node.GetProject());

    /// <summary>
    /// The raw <c>&lt;ModId&gt;</c> value for <paramref name="project" />: parsed out of the csproj XML
    /// (high-confidence route depending only on confirmed project-model + file-read capability).
    /// </summary>
    public static string? Resolve(IProject? project)
    {
        if (project == null)
            return null;

        return ReadModIdFromCsproj(project);
    }

    private static string? ReadModIdFromCsproj(IProject project)
    {
        var csproj = project.ProjectFileLocation;
        if (csproj.IsEmpty || !csproj.ExistsFile)
            return null;

        XDocument document;
        try
        {
            document = XDocument.Load(csproj.FullPath);
        }
        catch
        {
            return null;
        }

        var modId = document.Descendants()
            .Where(e => e.Name.LocalName == ModIdElementName)
            .Select(e => e.Value?.Trim())
            .LastOrDefault(v => !string.IsNullOrEmpty(v));

        return string.IsNullOrEmpty(modId) ? null : modId;
    }

    private static IProject? GetProject(IPsiModule? module) =>
        (module as IProjectPsiModule)?.Project;
}
