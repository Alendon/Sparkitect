using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;

namespace Sparkitect.RiderPlugin.ProjectModel;

/// <summary>
/// Registers the <c>.yaml</c> extension as a backend project file type handled by the YAML language.
/// The bundled ReSharper YAML support registers no file extensions of its own (it expects consumers to
/// register their own, the way the Unity plugin does for <c>.meta</c>/<c>.asset</c>), so without this no
/// <c>.yaml</c> file receives a YAML PSI on the backend — no daemon, no references, no navigation.
///
/// The platform keys its extension→file-type map on the LAST dotted segment only
/// (<c>foo.sparkres.yaml</c> → <c>.yaml</c>), so a <c>.sparkres.yaml</c> registration is never matched;
/// the extension must be <c>.yaml</c>. Deriving <see cref="YamlProjectFileType" /> reuses the existing
/// YAML parser/PSI and language service, so every <c>.yaml</c> file's <c>PrimaryPsiLanguage</c> becomes
/// YAML on the backend. Sparkitect-specific behaviour stays scoped to resource files because the daemon
/// stage and reference provider additionally gate on the <c>.sparkres.yaml</c> name suffix — plain
/// <c>.yaml</c> files get generic backend YAML analysis but no Sparkitect navigation.
/// </summary>
[ProjectFileTypeDefinition(Name)]
public class SparkresYamlProjectFileType : YamlProjectFileType
{
    public new const string Name = "SparkresYaml";

    [UsedImplicitly]
    public new static SparkresYamlProjectFileType? Instance { get; private set; }

    public SparkresYamlProjectFileType()
        : base(Name, "YAML (Sparkitect)", new[] { ".yaml" })
    {
    }
}
