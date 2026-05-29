using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Shared detection of the two navigable anchors in a <c>.sparkres.yaml</c> file:
/// the top-level registry-method key and the entry-ID scalar nested under a sequence entry.
/// Reused by the reference factory, the signal daemon, and the reverse-index builder so all three
/// agree on which scalars participate.
/// </summary>
public static class SparkresYamlAnchors
{
    /// <summary>The source text content of a scalar node.</summary>
    public static string? GetScalarText(IPlainScalarNode scalar) => scalar.GetText();

    /// <summary>
    /// True when the scalar is the key of a block-mapping entry at the document-root mapping level
    /// (the top-level <c>{registry-FQN}.{method}:</c> key) — i.e. not nested inside a sequence entry.
    /// </summary>
    public static bool IsTopLevelRegistryKey(IPlainScalarNode scalar)
    {
        var entry = BlockMappingEntryNavigator.GetByKey(scalar);
        return entry != null && GetEnclosingSequenceEntry(entry) == null;
    }

    /// <summary>
    /// True when the scalar is the key of a block-mapping entry that is a sequence element under a
    /// recognized top-level registry-method key; yields that registry's FQN.
    /// </summary>
    public static bool TryGetEntryIdAnchor(IPlainScalarNode scalar, out string? registryFqn)
    {
        registryFqn = null;

        var entry = BlockMappingEntryNavigator.GetByKey(scalar);
        if (entry == null)
            return false;

        var sequenceEntry = GetEnclosingSequenceEntry(entry);
        if (sequenceEntry == null)
            return false;

        var topKey = GetSequenceOwnerKey(sequenceEntry);
        if (topKey == null || !IsTopLevelRegistryKey(topKey))
            return false;

        registryFqn = GetScalarText(topKey);
        return !string.IsNullOrEmpty(registryFqn);
    }

    /// <summary>Mod prefix for a scalar's owning resource file (its project's name / root namespace).</summary>
    public static string? GetModPrefix(ITreeNode node) => node.GetProject()?.Name;

    private static ISequenceEntry? GetEnclosingSequenceEntry(ITreeNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current is ISequenceEntry sequenceEntry)
                return sequenceEntry;
            if (current is IYamlDocument)
                return null;
        }

        return null;
    }

    private static IPlainScalarNode? GetSequenceOwnerKey(ISequenceEntry sequenceEntry)
    {
        for (var current = sequenceEntry.Parent; current != null; current = current.Parent)
        {
            if (current is IBlockMappingEntry mappingEntry)
                return mappingEntry.Key as IPlainScalarNode;
            if (current is IYamlDocument)
                return null;
        }

        return null;
    }
}
