using JetBrains.Annotations;
using Sparkitect.DI;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Marks classes as Vulkan device configurator entrypoints.
/// </summary>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class VulkanDeviceConfiguratorEntrypointAttribute : Attribute;

/// <summary>
/// Entrypoint for mods to customize Vulkan logical device creation.
/// Implementations can enable device extensions, features, and configure queues.
/// </summary>
[PublicAPI]
public interface IVulkanDeviceConfigurator : IConfigurationEntrypoint<VulkanDeviceConfiguratorEntrypointAttribute>
{
    /// <summary>
    /// Configures the Vulkan device before creation.
    /// </summary>
    /// <param name="context">The device configuration context.</param>
    void Configure(IVulkanDeviceConfigurationContext context);
}
