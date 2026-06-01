using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Produces registration references on the two anchors of a <c>.sparkres.yaml</c> resource file: the
/// top-level registry-method key (<see cref="SparkresRegistryMethodReference" />) and the entry-ID
/// scalar nested under it (<see cref="SparkresEntryIdReference" />). The same anchor detection is shared
/// with the YAML signal layer and the reverse index.
/// </summary>
public class SparkresReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (element is not IPlainScalarNode scalar)
            return ReferenceCollection.Empty;

        if (SparkresYamlAnchors.IsTopLevelRegistryKey(scalar))
        {
            var fqn = SparkresYamlAnchors.GetScalarText(scalar);
            if (string.IsNullOrEmpty(fqn))
                return ReferenceCollection.Empty;

            return new ReferenceCollection(new SparkresRegistryMethodReference(scalar, fqn!));
        }

        if (SparkresYamlAnchors.TryGetEntryIdAnchor(scalar, out var registryFqn))
        {
            var entryId = SparkresYamlAnchors.GetScalarText(scalar);
            if (string.IsNullOrEmpty(entryId))
                return ReferenceCollection.Empty;

            var modId = SparkresYamlAnchors.GetModId(scalar);
            if (string.IsNullOrEmpty(modId))
                return ReferenceCollection.Empty;

            var category = SparkresYamlAnchors.GetRegistryCategory(scalar, registryFqn!);
            if (string.IsNullOrEmpty(category))
                return ReferenceCollection.Empty;

            var registration = RegistrationFactory.FromYamlEntry(scalar, modId!, category!, entryId!);
            if (registration == null)
                return ReferenceCollection.Empty;

            return new ReferenceCollection(new SparkresEntryIdReference(scalar, registration.Key));
        }

        return ReferenceCollection.Empty;
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element is not IPlainScalarNode scalar)
            return false;

        return SparkresYamlAnchors.IsTopLevelRegistryKey(scalar)
            || SparkresYamlAnchors.TryGetEntryIdAnchor(scalar, out _);
    }
}
