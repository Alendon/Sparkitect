//HintName: TestModule_TestRegistryRegistrations_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.Registrations;

using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.DI.Container;

[global::Sparkitect.DI.RegistrationsEntrypoint<global::StatelessTest.TestRegistry>]
public class TestModule_TestRegistryRegistrations_Providers : global::Sparkitect.DI.Registrations<global::StatelessTest.TestRegistry>
{
    public override string CategoryIdentifier => "test";

[global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Register_Init_Providers")]
    private static extern void __Reg_Init_Providers(
        global::TestMod.Generated.IdExtensions.TestModTestIDs _,
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager);

public override void ProcessRegistrations(global::StatelessTest.TestRegistry registry)
    {
__Reg_Init_Providers(default, registry, IdentificationManager, ResourceManager);
}
}
