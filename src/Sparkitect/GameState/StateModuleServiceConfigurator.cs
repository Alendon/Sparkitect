using Sparkitect.DI;

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
/// <remarks>
/// The <see cref="ICoreConfigurator{TDiscoveryAttribute}.Configure"/> method inherited from
/// <see cref="ICoreConfigurator{TDiscoveryAttribute}"/> registers module services.
/// </remarks>
public interface IStateModuleServiceConfigurator : ICoreConfigurator<StateModuleServiceConfiguratorEntrypointAttribute>
{
    /// <summary>
    /// Gets the module type this configurator registers services for.
    /// </summary>
    Type ModuleType { get; }
}
