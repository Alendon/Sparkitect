using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Highlightings;

/// <summary>
/// Daemon stage that styles Sparkitect registration identifiers: the inner content of a
/// registration ID literal, the use-site leaf identifier, and the auto-emit Identification segment.
/// </summary>
[DaemonStage]
public class SparkitectIdentificationDaemonStage : CSharpDaemonStageBase
{
    protected override IDaemonStageProcess CreateProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        DaemonProcessKind processKind,
        ICSharpFile file)
    {
        return new SparkitectIdentificationProcess(process, settings, file);
    }
}

internal class SparkitectIdentificationProcess : CSharpIncrementalDaemonStageProcessBase
{
    private const string IdExtensionsNamespaceMarker = ".CompilerGenerated.IdExtensions";
    private const string IdentificationMemberName = "Identification";
    private const string HasIdentificationFullName = "Sparkitect.Modding.IHasIdentification";

    public SparkitectIdentificationProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        ICSharpFile file)
        : base(process, settings, file)
    {
    }

    public override void Execute(System.Action<DaemonStageResult> committer)
    {
        var highlightings = new List<HighlightingInfo>();

        foreach (var node in File.ThisAndDescendants().ToEnumerable())
        {
            switch (node)
            {
                case ICSharpLiteralExpression literal:
                    AddLiteralContentHighlighting(literal, highlightings);
                    break;
                case IReferenceExpression reference:
                    AddUseSiteHighlighting(reference, highlightings);
                    break;
            }
        }

        committer(new DaemonStageResult(highlightings));
    }

    private static void AddLiteralContentHighlighting(
        ICSharpLiteralExpression literal,
        List<HighlightingInfo> highlightings)
    {
        if (!literal.IsConstantValue() || !literal.ConstantValue.IsString())
            return;

        var argument = CSharpArgumentNavigator.GetByValue(literal);
        if (argument == null)
            return;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return;

        var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!RegistrationMarkerPredicate.IsRegistrationAttribute(attributeType))
            return;

        var range = literal.GetStringLiteralContentTreeRange();
        AddHighlighting(literal, range, highlightings);
    }

    private void AddUseSiteHighlighting(
        IReferenceExpression reference,
        List<HighlightingInfo> highlightings)
    {
        var nameNode = reference.NameIdentifier;
        if (nameNode == null)
            return;

        var declaredElement = reference.Reference.Resolve().DeclaredElement;
        if (declaredElement is not IProperty property)
            return;

        if (property.ShortName == IdentificationMemberName)
        {
            if (IsHasIdentificationProperty(property))
                AddHighlighting(nameNode, nameNode.GetDocumentRange(), highlightings);
            return;
        }

        if (IsIdExtensionsProperty(property))
            AddHighlighting(nameNode, nameNode.GetDocumentRange(), highlightings);
    }

    private static bool IsIdExtensionsProperty(IProperty property)
    {
        var ns = property.GetContainingType()?.GetContainingNamespace().QualifiedName;
        return ns != null && ns.EndsWith(IdExtensionsNamespaceMarker);
    }

    private static bool IsHasIdentificationProperty(IProperty property)
    {
        var containing = property.GetContainingType();
        return containing != null
            && containing.GetAllSuperTypes()
                .Any(t => t.GetClrName().FullName == HasIdentificationFullName);
    }

    private static void AddHighlighting(
        ITreeNode node,
        DocumentRange range,
        List<HighlightingInfo> highlightings)
    {
        var highlighting = new SparkitectIdentificationHighlighting(node, range);
        highlightings.Add(new HighlightingInfo(range, highlighting));
    }

    private static void AddHighlighting(
        ITreeNode node,
        TreeTextRange treeRange,
        List<HighlightingInfo> highlightings)
    {
        var range = node.GetContainingFile()?.GetDocumentRange(treeRange);
        if (range == null || !range.Value.IsValid())
            return;

        var highlighting = new SparkitectIdentificationHighlighting(node, range.Value);
        highlightings.Add(new HighlightingInfo(range.Value, highlighting));
    }
}
