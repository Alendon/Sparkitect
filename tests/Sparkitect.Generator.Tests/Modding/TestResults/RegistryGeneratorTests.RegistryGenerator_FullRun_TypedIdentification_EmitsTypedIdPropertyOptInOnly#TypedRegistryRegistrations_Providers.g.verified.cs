//HintName: TypedRegistryRegistrations_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated.Registrations;

using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.DI.Container;

[global::Sparkitect.DI.RegistrationsEntrypoint<global::DiTest.TypedRegistry>]
public class TypedRegistryRegistrations_Providers : global::Sparkitect.DI.Registrations<global::DiTest.TypedRegistry>
{
    public override string CategoryIdentifier => "typed_registry";

[global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Register_TypedItem_Providers")]
    private static extern void __Reg_TypedItem_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedRegistryIDs _,
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Unregister_TypedItem_Providers")]
    private static extern void __Unreg_TypedItem_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedRegistryIDs _,
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager);

[global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Register_UntypedItem_Providers")]
    private static extern void __Reg_UntypedItem_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedRegistryIDs _,
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Unregister_UntypedItem_Providers")]
    private static extern void __Unreg_UntypedItem_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestTypedRegistryIDs _,
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager);

public override void ProcessRegistrations(global::DiTest.TypedRegistry registry)
    {
__Reg_TypedItem_Providers(default, registry, IdentificationManager, ResourceManager, Scope);
__Reg_UntypedItem_Providers(default, registry, IdentificationManager, ResourceManager, Scope);
}

    public override void ProcessUnregistrations(global::DiTest.TypedRegistry registry)
    {
__Unreg_TypedItem_Providers(default, registry, IdentificationManager, ResourceManager);
__Unreg_UntypedItem_Providers(default, registry, IdentificationManager, ResourceManager);
}
}
