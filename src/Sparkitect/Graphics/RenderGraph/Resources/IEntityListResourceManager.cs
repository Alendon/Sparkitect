using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Per-graph push target for the <see cref="EntityListResource"/>. Public by construction so a mod-assembly
/// staging pass can read the current published instance and the host buffer it must fill. Implements
/// <see cref="IGraphPushTargetFor{TResource}"/> so the type-routed push door dispatches to it.
/// </summary>
[PublicAPI]
public interface IEntityListResourceManager : IGraphPushTargetFor<EntityListResource>
{
    /// <summary>The current published entity list, or null before the first publish.</summary>
    EntityListResource? Current { get; }

    /// <summary>The host-visible/mapped staging buffer the staging pass memcpys into.</summary>
    VkBuffer HostBuffer { get; }
}
