using DryIoc;
using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect;

[CoreContainerConfiguratorEntrypoint]
public class SparkitectConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(IContainer container)
    {
        container.Register<IModManager, ModManager>();
    }
}