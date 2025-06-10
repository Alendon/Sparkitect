using System.Linq.Expressions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace MinimalSampleMod.DI;

[IRegistryConfigurator]
public class SampleModRegistryBuilder : IRegistryConfigurator
{
    public void ConfigureRegistries(IFactoryContainerBuilder<IRegistry> registryBuilder)
    {
        registryBuilder.Register(new DummyRegistry_KeyedFactory());
    }
}