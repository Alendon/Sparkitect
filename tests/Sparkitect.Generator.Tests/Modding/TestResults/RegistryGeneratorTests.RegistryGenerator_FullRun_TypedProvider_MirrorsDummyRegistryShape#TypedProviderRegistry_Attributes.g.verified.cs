//HintName: TypedProviderRegistry_Attributes.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591
#nullable enable

namespace DiTest;

public partial class TypedProviderRegistry
{
[global::Sparkitect.Modding.RegistrationMarker("typed_provider_registry")]
    public class RegisterTypedProviderAttribute([global::Sparkitect.Utilities.SnakeCase] string identifier) : global::System.Attribute, global::Sparkitect.Modding.IRegisterMarker
    {
        public bool GroupAtRoot { get; set; } = false;
    }
}
