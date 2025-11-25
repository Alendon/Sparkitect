using JetBrains.Annotations;
using Sparkitect.DI;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Marks a class as a Vulkan instance configurator entrypoint for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class VulkanInstanceConfiguratorEntrypointAttribute : Attribute;

/// <summary>
/// Configuration entrypoint for customizing Vulkan instance creation.
/// Implementations are discovered and invoked during VkInstance creation to add layers and extensions.
/// </summary>
[PublicAPI]
public interface IVulkanInstanceConfigurator : IConfigurationEntrypoint<VulkanInstanceConfiguratorEntrypointAttribute>
{
    /// <summary>
    /// Configures the Vulkan instance by adding layers and extensions.
    /// </summary>
    /// <param name="context">The configuration context providing available layers/extensions and methods to add them.</param>
    void Configure(IVulkanInstanceConfigurationContext context);
}
