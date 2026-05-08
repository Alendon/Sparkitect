//HintName: RenderPassRegistryRegistrations_Providers.g.cs
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

public static global::Sparkitect.Modding.Identification ClearColorPass { get; private set; }

    
    public override void ProcessRegistrations(global::DiTest.RenderPassRegistry registry)
    {
{
            ClearColorPass = IdentificationManager.RegisterObject("sample_test", "render_pass", "clear_color_pass");
registry.RegisterRenderPass<global::DiTest.ClearColorPass>(ClearColorPass);
        }
} 
}
