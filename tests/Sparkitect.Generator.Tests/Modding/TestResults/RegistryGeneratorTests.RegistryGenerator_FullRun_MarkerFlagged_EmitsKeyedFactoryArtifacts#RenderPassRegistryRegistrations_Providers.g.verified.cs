//HintName: RenderPassRegistryRegistrations_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated.Registrations;

using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.DI.Container;

[global::Sparkitect.DI.RegistrationsEntrypoint<global::DiTest.RenderPassRegistry>]
public class RenderPassRegistryRegistrations_Providers : global::Sparkitect.DI.Registrations<global::DiTest.RenderPassRegistry>
{
    public override string CategoryIdentifier => "render_pass";

[global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Register_ClearColorPass_Providers")]
    private static extern void __Reg_ClearColorPass_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestRenderPassIDs _,
        global::DiTest.RenderPassRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(
        global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod,
        Name = "Unregister_ClearColorPass_Providers")]
    private static extern void __Unreg_ClearColorPass_Providers(
        global::SampleTest.Generated.IdExtensions.SampleTestRenderPassIDs _,
        global::DiTest.RenderPassRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager);

public override void ProcessRegistrations(global::DiTest.RenderPassRegistry registry)
    {
__Reg_ClearColorPass_Providers(default, registry, IdentificationManager, ResourceManager, Scope);
}

    public override void ProcessUnregistrations(global::DiTest.RenderPassRegistry registry)
    {
__Unreg_ClearColorPass_Providers(default, registry, IdentificationManager, ResourceManager);
}
}
