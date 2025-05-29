using Sparkitect.DI;
using Sparkitect.DI.Container;

namespace MinimalSampleMod.DI;

[CoreContainerConfiguratorEntrypoint]
public class SampleModConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(ICoreContainerBuilder container)
    {
        container.Register<DummyValueManager_Factory>();
    }
}