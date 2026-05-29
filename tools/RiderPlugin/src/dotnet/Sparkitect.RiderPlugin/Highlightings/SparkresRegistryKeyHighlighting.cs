using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Highlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.Highlightings;

/// <summary>
/// Styles a <c>.sparkres.yaml</c> top-level registry-method key with the method-call theme color,
/// under the shared Sparkitect inspection entry (same configurable severity as every other
/// identification surface).
/// </summary>
[ConfigurableSeverityHighlighting(
    SparkitectIdentificationHighlighting.SeverityId,
    YamlLanguage.Name,
    AttributeId = CSharpHighlightingAttributeIds.METHOD_CALL,
    OverlapResolve = OverlapResolveKind.NONE)]
public class SparkresRegistryKeyHighlighting : IHighlighting
{
    private readonly ITreeNode myNode;
    private readonly DocumentRange myRange;

    public SparkresRegistryKeyHighlighting(ITreeNode node, DocumentRange range)
    {
        myNode = node;
        myRange = range;
    }

    public string ToolTip =>
        "Sparkitect identification (resource) — F12 navigates to the registration site; Find Usages lists all consumers.";

    public string ErrorStripeToolTip => ToolTip;

    public bool IsValid() => myNode.IsValid();

    public DocumentRange CalculateRange() => myRange;
}
