using Sparkitect.DI.Container;

namespace Sparkitect.DI;

/// <summary>
/// Interface for classes that configure the IoC container
/// </summary>
public abstract class CoreConfigurator 
    : IConfigurationEntrypoint<CoreContainerConfiguratorEntrypointAttribute>
{
    /// <summary>
    /// Configures the IoC container with services
    /// </summary>
    /// <param name="container">The container to configure</param>
    public abstract void ConfigureIoc(ICoreContainerBuilder container);
}