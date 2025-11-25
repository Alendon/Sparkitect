using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Descriptor for a retrieved Vulkan queue.
/// </summary>
[PublicAPI]
public sealed class VulkanQueue
{
    internal VulkanQueue(Queue handle, uint familyIndex, uint queueIndex, QueueFlags capabilities)
    {
        Handle = handle;
        FamilyIndex = familyIndex;
        QueueIndex = queueIndex;
        Capabilities = capabilities;
    }

    /// <summary>
    /// The native Vulkan queue handle.
    /// </summary>
    public Queue Handle { get; }

    /// <summary>
    /// The queue family this queue belongs to.
    /// </summary>
    public uint FamilyIndex { get; }

    /// <summary>
    /// The index of this queue within its family.
    /// </summary>
    public uint QueueIndex { get; }

    /// <summary>
    /// The capability flags of this queue's family.
    /// </summary>
    public QueueFlags Capabilities { get; }
}
