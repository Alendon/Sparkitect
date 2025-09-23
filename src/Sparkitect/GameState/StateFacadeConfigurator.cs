using Sparkitect.DI;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StateFacadeConfiguratorEntrypointAttribute : Attribute;

public abstract class StateFacadeConfigurator : ConfigurationEntrypoint<StateFacadeConfiguratorEntrypointAttribute>
{
    public abstract void ConfigureFacades(IStateContainerBuilder builder);
}

