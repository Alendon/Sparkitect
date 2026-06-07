namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Per-graph manager for push-descriptor <see cref="Descriptor"/> resources. Derives a
/// push-flagged <c>VkDescriptorSetLayout</c> from each declaration's binding config at Declare and
/// owns the layout lifetime. There is no <c>BeginFrame</c> hook — push descriptors allocate no pool
/// or set, so there is no per-frame set rotation to drive; pushes happen inline in pass Execute via
/// <see cref="Descriptor.Push"/>.
/// </summary>
internal interface IDescriptorResourceManager :
    IGraphResourceManager<Descriptor, DescriptorRequest>;
