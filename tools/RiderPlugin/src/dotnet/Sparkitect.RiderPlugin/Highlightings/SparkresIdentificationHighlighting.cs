using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Highlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.Highlightings;

/// <summary>
/// Styles a <c>.sparkres.yaml</c> entry-ID scalar with the class-identifier theme color — the same
/// color the C# leaf identifier uses — under the shared Sparkitect inspection entry, so one inspection
/// setting dismisses both surfaces.
/// </summary>
[ConfigurableSeverityHighlighting(
    SparkitectIdentificationHighlighting.SeverityId,
    YamlLanguage.Name,
    AttributeId = CSharpHighlightingAttributeIds.CLASS,
    OverlapResolve = OverlapResolveKind.NONE)]
public class SparkresIdentificationHighlighting : IHighlighting
{
    private readonly ITreeNode myNode;
    private readonly DocumentRange myRange;

    public SparkresIdentificationHighlighting(ITreeNode node, DocumentRange range)
    {
        myNode = node;
        myRange = range;
    }

    public string ToolTip =>
        "Sparkitect registration identifier (resource) — Go to Declaration and Find Usages follow it to the generated id; Navigate To also offers the registration site.";

    public string ErrorStripeToolTip => ToolTip;

    public bool IsValid() => myNode.IsValid();

    public DocumentRange CalculateRange() => myRange;
}
