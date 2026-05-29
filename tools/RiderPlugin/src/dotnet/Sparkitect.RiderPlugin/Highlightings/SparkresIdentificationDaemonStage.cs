using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Highlightings;

/// <summary>
/// Daemon stage that styles the two Sparkitect anchors in a <c>.sparkres.yaml</c> file: the entry-ID
/// scalar (class-identifier color) and the top-level registry-method key (method-call color), reusing
/// the shared anchor detection.
/// </summary>
[DaemonStage(StagesBefore = [typeof(IdentifierHighlightingStage)])]
public class SparkresIdentificationDaemonStage : YamlDaemonStageBase
{
    protected override IDaemonStageProcess CreateProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        DaemonProcessKind processKind,
        IYamlFile file)
    {
        return new SparkresIdentificationProcess(process, settings, file);
    }
}

internal class SparkresIdentificationProcess : IDaemonStageProcess
{
    private const string SparkresSuffix = ".sparkres.yaml";
    private readonly IYamlFile myFile;

    public SparkresIdentificationProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        IYamlFile file)
    {
        DaemonProcess = process;
        myFile = file;
    }

    public IDaemonProcess DaemonProcess { get; }

    public void Execute(Action<DaemonStageResult> committer)
    {
        if (!DaemonProcess.SourceFile.Name.EndsWith(SparkresSuffix))
            return;

        var highlightings = new List<HighlightingInfo>();

        foreach (var scalar in myFile.ThisAndDescendants().OfType<IPlainScalarNode>().ToEnumerable())
        {
            if (SparkresYamlAnchors.IsTopLevelRegistryKey(scalar))
            {
                AddHighlighting(scalar, new SparkresRegistryKeyHighlighting(scalar, scalar.GetDocumentRange()), highlightings);
            }
            else if (SparkresYamlAnchors.TryGetEntryIdAnchor(scalar, out _))
            {
                AddHighlighting(scalar, new SparkresIdentificationHighlighting(scalar, scalar.GetDocumentRange()), highlightings);
            }
        }

        committer(new DaemonStageResult(highlightings));
    }

    private static void AddHighlighting(ITreeNode node, IHighlighting highlighting, List<HighlightingInfo> highlightings)
    {
        var range = node.GetDocumentRange();
        if (range.IsValid())
            highlightings.Add(new HighlightingInfo(range, highlighting));
    }
}
