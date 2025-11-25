using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Represents a Vulkan queue with its associated metadata.
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
    /// Gets the native Vulkan queue handle.
    /// </summary>
    public Queue Handle { get; }

    /// <summary>
    /// Gets the queue family index this queue belongs to.
    /// </summary>
    public uint FamilyIndex { get; }

    /// <summary>
    /// Gets the index of this queue within its family.
    /// </summary>
    public uint QueueIndex { get; }

    /// <summary>
    /// Gets the capability flags of this queue's family.
    /// </summary>
    public QueueFlags Capabilities { get; }

    /// <summary>
    /// Returns true if this queue supports graphics operations.
    /// </summary>
    public bool SupportsGraphics => (Capabilities & QueueFlags.GraphicsBit) != 0;

    /// <summary>
    /// Returns true if this queue supports compute operations.
    /// </summary>
    public bool SupportsCompute => (Capabilities & QueueFlags.ComputeBit) != 0;

    /// <summary>
    /// Returns true if this queue supports transfer operations.
    /// </summary>
    public bool SupportsTransfer => (Capabilities & QueueFlags.TransferBit) != 0;

    /// <summary>
    /// Returns true if this queue supports sparse binding operations.
    /// </summary>
    public bool SupportsSparseBinding => (Capabilities & QueueFlags.SparseBindingBit) != 0;
}
