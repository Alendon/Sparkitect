using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect;

[CoreContainerConfiguratorEntrypoint]
public class SparkitectConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(ICoreContainerBuilder container)
    {
        container.Register<RegistryManager_Factory>();
    }
}