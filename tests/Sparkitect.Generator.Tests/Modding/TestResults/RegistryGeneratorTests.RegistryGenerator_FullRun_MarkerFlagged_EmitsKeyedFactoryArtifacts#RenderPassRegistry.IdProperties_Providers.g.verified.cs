//HintName: RenderPassRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated.IdExtensions;

public readonly partial struct SampleTestRenderPassIDs
{
private static global::Sparkitect.Modding.Identification _clearColorPass_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::DiTest.ClearColorPass))]
    public global::Sparkitect.Modding.Identification ClearColorPass => _clearColorPass_Providers;

    private static void Register_ClearColorPass_Providers(
        global::DiTest.RenderPassRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _clearColorPass_Providers = identificationManager.RegisterObject("sample_test", "render_pass", "clear_color_pass");
registry.RegisterRenderPass<global::DiTest.ClearColorPass>(_clearColorPass_Providers);
    }
}
