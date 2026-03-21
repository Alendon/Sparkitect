using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.Metadata;

/// <summary>
/// Discovery attribute for metadata entrypoints -- one per TMetadata type.
/// </summary>
public class ApplyMetadataEntrypointAttribute<TMetadata> : Attribute;

/// <summary>
/// Base class for generated (or hand-written) metadata entrypoints.
/// Plugs into EntrypointContainer&lt;ApplyMetadataEntrypoint&lt;TMetadata&gt;&gt; unchanged.
/// </summary>
/// <typeparam name="TMetadata">The metadata type collected by this entrypoint.</typeparam>
public abstract class ApplyMetadataEntrypoint<TMetadata>
    : IConfigurationEntrypoint<ApplyMetadataEntrypointAttribute<TMetadata>>
{
    /// <summary>
    /// Collects metadata from decorated types/methods into the provided dictionary.
    /// </summary>
    /// <param name="metadata">Dictionary mapping identifications to their metadata instances.</param>
    public abstract void CollectMetadata(Dictionary<Identification, TMetadata> metadata);
}
