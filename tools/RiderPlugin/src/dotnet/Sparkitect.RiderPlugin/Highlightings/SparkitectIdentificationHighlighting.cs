using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Highlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.Highlightings;

/// <summary>
/// Styles a Sparkitect registration identifier span and its usages with the class-identifier
/// theme color. Dismissable via Settings → Editor → Inspection Settings → Sparkitect.
/// </summary>
[RegisterConfigurableSeverity(
    SeverityId,
    null,
    "Sparkitect",
    "Sparkitect identification marker",
    "Marks Sparkitect registration identifiers and their usages.",
    Severity.HINT)]
[ConfigurableSeverityHighlighting(
    SeverityId,
    CSharpLanguage.Name,
    AttributeId = CSharpHighlightingAttributeIds.CLASS,
    OverlapResolve = OverlapResolveKind.NONE)]
public class SparkitectIdentificationHighlighting : IHighlighting
{
    /// <summary>Shared configurable-severity id governing both the C# and YAML signal surfaces.</summary>
    public const string SeverityId = "SparkitectIdentification";

    private readonly ITreeNode myNode;
    private readonly DocumentRange myRange;

    public SparkitectIdentificationHighlighting(ITreeNode node, DocumentRange range)
    {
        myNode = node;
        myRange = range;
    }

    public string ToolTip =>
        "Sparkitect registration identifier — Go to Declaration and Find Usages follow it to the generated id; Navigate To also offers the registration site.";

    public string ErrorStripeToolTip => ToolTip;

    public bool IsValid() => myNode.IsValid();

    public DocumentRange CalculateRange() => myRange;
}
