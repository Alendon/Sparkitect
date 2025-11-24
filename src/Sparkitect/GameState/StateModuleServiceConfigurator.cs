using Sparkitect.DI;
using Sparkitect.DI.Container;

namespace Sparkitect.GameState;

/// <summary>
/// Marks state module service configurators for source generator discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateModuleServiceConfiguratorEntrypointAttribute : Attribute;

/// <summary>
/// Configuration entrypoint for registering module-scoped services. Implementations source-generated
/// (marked [CompilerGenerated]) when [StateService] attributes are used.
/// </summary>
public interface IStateModuleServiceConfigurator : IConfigurationEntrypoint<StateModuleServiceConfiguratorEntrypointAttribute>
{
    /// <summary>
    /// Gets the module type this configurator registers services for.
    /// </summary>
    Type ModuleType { get; }

    /// <summary>
    /// Registers module services with the container builder.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    void ConfigureServices(ICoreContainerBuilder builder);
}
