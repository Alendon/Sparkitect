using Sparkitect.Modding;

namespace Sparkitect.DI;


public interface IIoCRegistryBuilder : ConfigurationEntrypoint<IoCRegistryBuilderEntrypointAttribute>
{
    
    void ConfigureRegistries(IRegistryProxy registryProxy);
}

public interface IRegistryProxy
{
    void AddRegistry<TRegistry>(string categoryIdentifier) where TRegistry : IRegistry;
}