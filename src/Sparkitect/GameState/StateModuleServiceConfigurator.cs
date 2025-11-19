using Sparkitect.DI;
using Sparkitect.DI.Container;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StateModuleServiceConfiguratorEntrypointAttribute : Attribute;

public interface IStateModuleServiceConfigurator : IConfigurationEntrypoint<StateModuleServiceConfiguratorEntrypointAttribute>
{
    Type ModuleType { get; }
    void ConfigureServices(ICoreContainerBuilder builder);
}
