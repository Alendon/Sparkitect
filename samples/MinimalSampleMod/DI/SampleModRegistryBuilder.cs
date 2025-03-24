using Sparkitect.DI;

namespace MinimalSampleMod.DI;

[IoCRegistryBuilderEntrypoint]
public class SampleModRegistryBuilder : IIoCRegistryBuilder
{
    public void ConfigureRegistries(IRegistryProxy registryProxy)
    {
        registryProxy.AddRegistry<DummyRegistry>("dummy_values");
    }
}