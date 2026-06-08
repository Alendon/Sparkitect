using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Metadata;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Layer-1 metadata entrypoint for the stock <see cref="Runtime.RenderGraph"/>. Declares the per-graph
/// required services list: per-instance image, descriptor, device-buffer, and entity-list managers.
/// </summary>
[ApplyMetadataEntrypoint<RGServiceListMetadata>]
internal sealed class MainRGServiceList : ApplyMetadataEntrypoint<RGServiceListMetadata>
{
    public override void CollectMetadata(Dictionary<Identification, RGServiceListMetadata> metadata)
    {
        metadata[Runtime.RenderGraph.Identification] =
            new RGServiceListMetadata([
                typeof(IImageResourceManager),
                typeof(IDescriptorResourceManager),
                typeof(IBufferResourceManager),
                typeof(IEntityListResourceManager),
            ]);
    }
}
