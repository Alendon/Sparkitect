using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.DI;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Marks a class as a Vulkan physical device selector entrypoint for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class VulkanPhysicalDeviceSelectorEntrypointAttribute : Attribute;

/// <summary>
/// Configuration entrypoint for customizing Vulkan physical device selection.
/// Implementations are discovered and invoked during physical device selection to influence the choice.
/// </summary>
[PublicAPI]
public interface IVulkanPhysicalDeviceSelector : IConfigurationEntrypoint<VulkanPhysicalDeviceSelectorEntrypointAttribute>
{
    /// <summary>
    /// Scores a physical device for selection. Higher scores are preferred.
    /// Return null to use default scoring, or a value to override.
    /// </summary>
    /// <param name="context">Context providing device information.</param>
    /// <returns>Score for this device, or null to use default scoring.</returns>
    int? ScoreDevice(IVulkanPhysicalDeviceSelectionContext context);
}

/// <summary>
/// Context for querying physical device information during selection.
/// </summary>
[PublicAPI]
public interface IVulkanPhysicalDeviceSelectionContext
{
    /// <summary>
    /// The physical device handle being evaluated.
    /// </summary>
    PhysicalDevice Device { get; }

    /// <summary>
    /// Gets the properties of this physical device.
    /// </summary>
    PhysicalDeviceProperties Properties { get; }

    /// <summary>
    /// Gets the features of this physical device.
    /// </summary>
    PhysicalDeviceFeatures Features { get; }

    /// <summary>
    /// Gets the memory properties of this physical device.
    /// </summary>
    PhysicalDeviceMemoryProperties MemoryProperties { get; }

    /// <summary>
    /// Gets the queue family properties of this physical device.
    /// </summary>
    QueueFamilyProperties[] QueueFamilyProperties { get; }
}

internal sealed class VulkanPhysicalDeviceSelectionContext : IVulkanPhysicalDeviceSelectionContext
{
    private readonly Vk _vk;
    private PhysicalDeviceProperties? _properties;
    private PhysicalDeviceFeatures? _features;
    private PhysicalDeviceMemoryProperties? _memoryProperties;
    private QueueFamilyProperties[]? _queueFamilyProperties;

    public VulkanPhysicalDeviceSelectionContext(Vk vk, PhysicalDevice device)
    {
        _vk = vk;
        Device = device;
    }

    public PhysicalDevice Device { get; }

    public PhysicalDeviceProperties Properties =>
        _properties ??= _vk.GetPhysicalDeviceProperties(Device);

    public PhysicalDeviceFeatures Features =>
        _features ??= _vk.GetPhysicalDeviceFeatures(Device);

    public PhysicalDeviceMemoryProperties MemoryProperties =>
        _memoryProperties ??= _vk.GetPhysicalDeviceMemoryProperties(Device);

    public unsafe QueueFamilyProperties[] QueueFamilyProperties
    {
        get
        {
            if (_queueFamilyProperties != null) return _queueFamilyProperties;

            uint count = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(Device, ref count, null);
            if (count == 0) return _queueFamilyProperties = [];

            _queueFamilyProperties = new QueueFamilyProperties[count];
            fixed (QueueFamilyProperties* ptr = _queueFamilyProperties)
            {
                _vk.GetPhysicalDeviceQueueFamilyProperties(Device, ref count, ptr);
            }

            return _queueFamilyProperties;
        }
    }
}
