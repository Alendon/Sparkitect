using JetBrains.Annotations;
using Sparkitect.Events;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// The engine's own Vulkan event declarations. Each provider is registered through the standard
/// <see cref="EventRegistry"/> path; the closed generic payload type survives registration and reaches
/// the facade with its type intact, so <c>EventID.Sparkitect.VulkanInstanceConfiguring</c> is typed
/// <c>Identification&lt;IVulkanInstanceConfigurationContext&gt;</c>.
/// </summary>
[PublicAPI]
public static class VulkanEventDeclarations
{
    /// <summary>Published by <c>VulkanContext.CreateInstance</c> before the engine's own inline instance config; subscribers mutate the context.</summary>
    [EventRegistry.RegisterEvent("vulkan_instance_configuring")]
    public static IEventDefinition<IVulkanInstanceConfigurationContext> VulkanInstanceConfiguring => new EventDefinition<IVulkanInstanceConfigurationContext>();

    /// <summary>Published by <c>VulkanContext.CreateDevice</c> before the engine's own inline device config; subscribers mutate the context.</summary>
    [EventRegistry.RegisterEvent("vulkan_device_configuring")]
    public static IEventDefinition<IVulkanDeviceConfigurationContext> VulkanDeviceConfiguring => new EventDefinition<IVulkanDeviceConfigurationContext>();
}
