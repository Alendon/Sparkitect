//HintName: TestRegistryRegistrations_Providers.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.Registrations;

using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.DI.Container;

[global::Sparkitect.DI.RegistrationsEntrypoint<global::StatelessTest.TestRegistry>]
public class TestRegistryRegistrations_Providers : global::Sparkitect.DI.Registrations<global::StatelessTest.TestRegistry>
{
    public override string CategoryIdentifier => "test";

public static global::Sparkitect.Modding.Identification Init { get; private set; }

    
    public override void ProcessRegistrations(global::StatelessTest.TestRegistry registry)
    {
{
            Init = IdentificationManager.RegisterObject("test_mod", "test", "init");
registry.Register<global::TestMod.TestModule.InitFunc>(Init);
        }
} 
}
