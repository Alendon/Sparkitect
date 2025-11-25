using JetBrains.Annotations;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Read-only context for querying and adding Vulkan instance layers and extensions.
/// </summary>
[PublicAPI]
public interface IVulkanInstanceConfigurationContext
{
    /// <summary>
    /// Gets the names of all available instance layers.
    /// </summary>
    IReadOnlyList<string> AvailableLayers { get; }

    /// <summary>
    /// Gets the names of all available instance extensions.
    /// </summary>
    IReadOnlyList<string> AvailableExtensions { get; }

    /// <summary>
    /// Checks if a layer is available.
    /// </summary>
    bool IsLayerAvailable(string layerName);

    /// <summary>
    /// Checks if an extension is available.
    /// </summary>
    bool IsExtensionAvailable(string extensionName);

    /// <summary>
    /// Adds a layer to be enabled. Ignored if the layer is unavailable or already added.
    /// </summary>
    /// <param name="layerName">The layer name to add.</param>
    /// <returns>True if the layer was added; false if unavailable or duplicate.</returns>
    bool AddLayer(string layerName);

    /// <summary>
    /// Adds an extension to be enabled. Ignored if the extension is unavailable or already added.
    /// </summary>
    /// <param name="extensionName">The extension name to add.</param>
    /// <returns>True if the extension was added; false if unavailable or duplicate.</returns>
    bool AddExtension(string extensionName);
}

/// <summary>
/// Internal implementation of the Vulkan instance configuration context.
/// </summary>
internal sealed class VulkanInstanceConfigurationContext : IVulkanInstanceConfigurationContext
{
    private readonly HashSet<string> _availableLayers;
    private readonly HashSet<string> _availableExtensions;
    private readonly HashSet<string> _enabledLayers = [];
    private readonly HashSet<string> _enabledExtensions = [];

    public VulkanInstanceConfigurationContext(Vk vk)
    {
        _availableLayers = QueryAvailableLayers(vk);
        _availableExtensions = QueryAvailableExtensions(vk);
    }

    public IReadOnlyList<string> AvailableLayers => _availableLayers.ToList();
    public IReadOnlyList<string> AvailableExtensions => _availableExtensions.ToList();

    public bool IsLayerAvailable(string layerName) => _availableLayers.Contains(layerName);
    public bool IsExtensionAvailable(string extensionName) => _availableExtensions.Contains(extensionName);

    public bool AddLayer(string layerName)
    {
        if (!_availableLayers.Contains(layerName)) return false;
        return _enabledLayers.Add(layerName);
    }

    public bool AddExtension(string extensionName)
    {
        if (!_availableExtensions.Contains(extensionName)) return false;
        return _enabledExtensions.Add(extensionName);
    }

    internal IReadOnlyList<string> GetEnabledLayers() => _enabledLayers.ToList();
    internal IReadOnlyList<string> GetEnabledExtensions() => _enabledExtensions.ToList();

    private static unsafe HashSet<string> QueryAvailableLayers(Vk vk)
    {
        uint count = 0;
        vk.EnumerateInstanceLayerProperties(ref count, null);

        if (count == 0) return [];

        var properties = new LayerProperties[count];
        fixed (LayerProperties* ptr = properties)
        {
            vk.EnumerateInstanceLayerProperties(ref count, ptr);
        }

        var layers = new HashSet<string>((int)count);
        for (var i = 0; i < count; i++)
        {
            fixed (LayerProperties* propPtr = &properties[i])
            {
                var name = SilkMarshal.PtrToString((nint)propPtr->LayerName);
                if (name != null) layers.Add(name);
            }
        }

        return layers;
    }

    private static unsafe HashSet<string> QueryAvailableExtensions(Vk vk)
    {
        uint count = 0;
        vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, null);

        if (count == 0) return [];

        var properties = new ExtensionProperties[count];
        fixed (ExtensionProperties* ptr = properties)
        {
            vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, ptr);
        }

        var extensions = new HashSet<string>((int)count);
        for (var i = 0; i < count; i++)
        {
            fixed (ExtensionProperties* propPtr = &properties[i])
            {
                var name = SilkMarshal.PtrToString((nint)propPtr->ExtensionName);
                if (name != null) extensions.Add(name);
            }
        }

        return extensions;
    }
}
