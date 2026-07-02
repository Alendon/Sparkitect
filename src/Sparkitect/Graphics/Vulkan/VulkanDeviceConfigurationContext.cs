using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

internal record QueueFamilyRequest(uint QueueFamilyIndex, uint QueueCount, float[] Priorities);

/// <summary>
/// Context for configuring Vulkan device creation.
/// </summary>
[PublicAPI]
public interface IVulkanDeviceConfigurationContext
{
    /// <summary>The device extensions available on the selected physical device.</summary>
    IReadOnlyList<string> AvailableExtensions { get; }

    /// <summary>The queue family properties of the selected physical device.</summary>
    IReadOnlyList<QueueFamilyProperties> QueueFamilyProperties { get; }

    /// <summary>The core features the physical device supports.</summary>
    PhysicalDeviceFeatures AvailableFeatures { get; }

    /// <summary>The Vulkan 1.1 features the physical device supports.</summary>
    PhysicalDeviceVulkan11Features AvailableFeatures11 { get; }

    /// <summary>The Vulkan 1.2 features the physical device supports.</summary>
    PhysicalDeviceVulkan12Features AvailableFeatures12 { get; }

    /// <summary>The Vulkan 1.3 features the physical device supports.</summary>
    PhysicalDeviceVulkan13Features AvailableFeatures13 { get; }

    /// <summary>The core features to enable on the device; mods mutate this in place.</summary>
    ref PhysicalDeviceFeatures EnabledFeatures { get; }

    /// <summary>The Vulkan 1.1 features to enable; mods mutate this in place.</summary>
    ref PhysicalDeviceVulkan11Features EnabledFeatures11 { get; }

    /// <summary>The Vulkan 1.2 features to enable; mods mutate this in place.</summary>
    ref PhysicalDeviceVulkan12Features EnabledFeatures12 { get; }

    /// <summary>The Vulkan 1.3 features to enable; mods mutate this in place.</summary>
    ref PhysicalDeviceVulkan13Features EnabledFeatures13 { get; }

    /// <summary>Whether the named device extension is available.</summary>
    bool IsExtensionAvailable(string extensionName);

    /// <summary>Enables the named device extension; returns false if unavailable or already enabled.</summary>
    bool AddExtension(string extensionName);

    /// <summary>Requests <paramref name="count"/> queues from <paramref name="queueFamilyIndex"/>, replacing any prior request for that family.</summary>
    void RequestQueues(uint queueFamilyIndex, uint count, float[]? priorities = null);

    /// <summary>Returns the closest-matching queue family index for <paramref name="requiredFlags"/>, or null if none qualifies.</summary>
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
