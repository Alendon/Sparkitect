//HintName: RegistryConfigurator.g.cs
namespace DiTest.Generated;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.RegistryConfigurator]
public class RegistryConfigurator : global::Sparkitect.DI.IRegistryConfigurator
{
    public void ConfigureRegistries(global::Sparkitect.DI.Container.IFactoryContainerBuilder<global::Sparkitect.Modding.IRegistry> registryBuilder)
    {
        registryBuilder.Register(new global::DiTest.TestRegistry1_KeyedFactory());
        registryBuilder.Register(new global::DiTest.TestRegistry2_KeyedFactory());
    }
}