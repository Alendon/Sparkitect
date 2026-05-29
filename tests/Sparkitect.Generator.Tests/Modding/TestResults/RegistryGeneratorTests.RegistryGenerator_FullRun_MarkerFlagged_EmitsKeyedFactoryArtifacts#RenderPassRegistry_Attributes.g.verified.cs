//HintName: RenderPassRegistry_Attributes.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591
#nullable enable

namespace DiTest;

public partial class RenderPassRegistry
{
[global::Sparkitect.Modding.RegistrationMarker("render_pass")]
    public class RegisterRenderPassAttribute([global::Sparkitect.Utilities.SnakeCase] string identifier) : global::System.Attribute, global::Sparkitect.Modding.IRegisterMarker
    {
        public bool GroupAtRoot { get; set; } = false;
    }
}
