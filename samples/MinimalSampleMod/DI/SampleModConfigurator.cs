using DryIoc;
using Sparkitect.DI;

namespace MinimalSampleMod.DI;

[CoreContainerConfiguratorEntrypoint]
public class SampleModConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(IContainer container)
    {
        container.Register<IDummyValueManager, DummyValueManager>();
    }
}