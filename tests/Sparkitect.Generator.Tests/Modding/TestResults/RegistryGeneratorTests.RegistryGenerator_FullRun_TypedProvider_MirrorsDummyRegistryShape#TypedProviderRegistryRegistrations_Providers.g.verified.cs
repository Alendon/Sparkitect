//HintName: TypedProviderRegistryRegistrations_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated.Registrations;

using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.DI.Container;

[global::Sparkitect.DI.RegistrationsEntrypoint<global::DiTest.TypedProviderRegistry>]
public class TypedProviderRegistryRegistrations_Providers : global::Sparkitect.DI.Registrations<global::DiTest.TypedProviderRegistry>
{
    public override string CategoryIdentifier => "typed_provider_registry";

[global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Register_TypedDummyProvider_Providers")]
    private static extern void __Reg_TypedDummyProvider_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedProviderRegistryIDs _,
        global::DiTest.TypedProviderRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Unregister_TypedDummyProvider_Providers")]
    private static extern void __Unreg_TypedDummyProvider_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedProviderRegistryIDs _,
        global::DiTest.TypedProviderRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager);

public override void ProcessRegistrations(global::DiTest.TypedProviderRegistry registry)
    {
__Reg_TypedDummyProvider_Providers(default, registry, IdentificationManager, ResourceManager, Scope);
}

    public override void ProcessUnregistrations(global::DiTest.TypedProviderRegistry registry)
    {
__Unreg_TypedDummyProvider_Providers(default, registry, IdentificationManager, ResourceManager);
}
}
