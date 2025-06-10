using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;


public interface IRegistryConfigurator : ConfigurationEntrypoint<IRegistryConfiguratorAttribute>
{
    void ConfigureRegistries(IFactoryContainerBuilder<IRegistry> registryBuilder);
}


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class IRegistryConfiguratorAttribute : Attribute;