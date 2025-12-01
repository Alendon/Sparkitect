using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Represents a queue family request for device creation.
/// </summary>
[PublicAPI]
public record QueueFamilyRequest(uint QueueFamilyIndex, uint QueueCount, float[] Priorities);

/// <summary>
/// Context for configuring Vulkan device creation.
/// </summary>
[PublicAPI]
public interface IVulkanDeviceConfigurationContext
{
    IReadOnlyList<string> AvailableExtensions { get; }
    IReadOnlyList<QueueFamilyProperties> QueueFamilyProperties { get; }

    // Available features (read-only, queried from physical device)
    PhysicalDeviceFeatures AvailableFeatures { get; }
    PhysicalDeviceVulkan11Features AvailableFeatures11 { get; }
    PhysicalDeviceVulkan12Features AvailableFeatures12 { get; }
    PhysicalDeviceVulkan13Features AvailableFeatures13 { get; }

    // Enabled features (mutable, set by mods)
    ref PhysicalDeviceFeatures EnabledFeatures { get; }
    ref PhysicalDeviceVulkan11Features EnabledFeatures11 { get; }
    ref PhysicalDeviceVulkan12Features EnabledFeatures12 { get; }
    ref PhysicalDeviceVulkan13Features EnabledFeatures13 { get; }

    bool IsExtensionAvailable(string extensionName);
    bool AddExtension(string extensionName);
    void RequestQueues(uint queueFamilyIndex, uint count, float[]? priorities = null);
    uint? FindQueueFamily(QueueFlags requiredFlags);
}

internal sealed class VulkanDeviceConfigurationContext : IVulkanDeviceConfigurationContext
{
    private readonly HashSet<string> _availableExtensions;
    private readonly HashSet<string> _enabledExtensions = [];
    private readonly List<QueueFamilyRequest> _queueRequests = [];
    private readonly QueueFamilyProperties[] _queueFamilyProperties;

    private PhysicalDeviceFeatures _enabledFeatures;
    private PhysicalDeviceVulkan11Features _enabledFeatures11 = new() { SType = StructureType.PhysicalDeviceVulkan11Features };
    private PhysicalDeviceVulkan12Features _enabledFeatures12 = new() { SType = StructureType.PhysicalDeviceVulkan12Features };
    private PhysicalDeviceVulkan13Features _enabledFeatures13 = new() { SType = StructureType.PhysicalDeviceVulkan13Features };

    public unsafe VulkanDeviceConfigurationContext(Vk vk, PhysicalDevice physicalDevice)
    {
        _availableExtensions = QueryAvailableExtensions(vk, physicalDevice);
        _queueFamilyProperties = QueryQueueFamilyProperties(vk, physicalDevice);

        var features13 = new PhysicalDeviceVulkan13Features { SType = StructureType.PhysicalDeviceVulkan13Features };
        var features12 = new PhysicalDeviceVulkan12Features { SType = StructureType.PhysicalDeviceVulkan12Features, PNext = &features13 };
        var features11 = new PhysicalDeviceVulkan11Features { SType = StructureType.PhysicalDeviceVulkan11Features, PNext = &features12 };
        var features2 = new PhysicalDeviceFeatures2 { SType = StructureType.PhysicalDeviceFeatures2, PNext = &features11 };

        vk.GetPhysicalDeviceFeatures2(physicalDevice, &features2);

        AvailableFeatures = features2.Features;
        AvailableFeatures11 = features11;
        AvailableFeatures12 = features12;
        AvailableFeatures13 = features13;
    }

    public IReadOnlyList<string> AvailableExtensions => _availableExtensions.ToList();
    public IReadOnlyList<QueueFamilyProperties> QueueFamilyProperties => _queueFamilyProperties;

    public PhysicalDeviceFeatures AvailableFeatures { get; }
    public PhysicalDeviceVulkan11Features AvailableFeatures11 { get; }
    public PhysicalDeviceVulkan12Features AvailableFeatures12 { get; }
    public PhysicalDeviceVulkan13Features AvailableFeatures13 { get; }

    public ref PhysicalDeviceFeatures EnabledFeatures => ref _enabledFeatures;
    public ref PhysicalDeviceVulkan11Features EnabledFeatures11 => ref _enabledFeatures11;
    public ref PhysicalDeviceVulkan12Features EnabledFeatures12 => ref _enabledFeatures12;
    public ref PhysicalDeviceVulkan13Features EnabledFeatures13 => ref _enabledFeatures13;

    public bool IsExtensionAvailable(string extensionName) => _availableExtensions.Contains(extensionName);
    public bool AddExtension(string extensionName) => _availableExtensions.Contains(extensionName) && _enabledExtensions.Add(extensionName);

    public void RequestQueues(uint queueFamilyIndex, uint count, float[]? priorities = null)
    {
        if (queueFamilyIndex >= _queueFamilyProperties.Length)
            throw new ArgumentOutOfRangeException(nameof(queueFamilyIndex));

        priorities ??= Enumerable.Repeat(1.0f, (int)count).ToArray();
        _queueRequests.RemoveAll(r => r.QueueFamilyIndex == queueFamilyIndex);
        _queueRequests.Add(new QueueFamilyRequest(queueFamilyIndex, count, priorities));
    }

    public uint? FindQueueFamily(QueueFlags requiredFlags)
    {
        var result = uint.MaxValue;
        var shortestDistance = int.MaxValue;
        
        
        for (uint i = 0; i < _queueFamilyProperties.Length; i++)
            if ((_queueFamilyProperties[i].QueueFlags & requiredFlags) == requiredFlags)
            {
                var queuePCount =
                    BitOperations.PopCount(Unsafe.As<QueueFlags, uint>(ref _queueFamilyProperties[i].QueueFlags));
                var reqPCount = BitOperations.PopCount(Unsafe.As<QueueFlags, uint>(ref requiredFlags));
                var distance = queuePCount - reqPCount;
                
                if (shortestDistance > distance)
                {
                    result = i;
                    shortestDistance = distance;
                }
            }

        if (result != uint.MaxValue) return result;
        return null;
    }

    internal IReadOnlyList<string> GetEnabledExtensions() => _enabledExtensions.ToList();
    internal IReadOnlyList<QueueFamilyRequest> GetQueueRequests() => _queueRequests;

    private static unsafe HashSet<string> QueryAvailableExtensions(Vk vk, PhysicalDevice physicalDevice)
    {
        uint count = 0;
        vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref count, null);
        if (count == 0) return [];

        var properties = new ExtensionProperties[count];
        fixed (ExtensionProperties* ptr = properties)
            vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref count, ptr);

        var extensions = new HashSet<string>((int)count);
        for (var i = 0; i < count; i++)
            fixed (ExtensionProperties* propPtr = &properties[i])
                if (SilkMarshal.PtrToString((nint)propPtr->ExtensionName) is { } name)
                    extensions.Add(name);
        return extensions;
    }

    private static QueueFamilyProperties[] QueryQueueFamilyProperties(Vk vk, PhysicalDevice physicalDevice)
    {
        Span<uint> count = [0];
        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, count, []);
        if (count[0] == 0) return [];

        var properties = new QueueFamilyProperties[count[0]];

        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, count, properties.AsSpan());
        
        return properties;
    }
}
