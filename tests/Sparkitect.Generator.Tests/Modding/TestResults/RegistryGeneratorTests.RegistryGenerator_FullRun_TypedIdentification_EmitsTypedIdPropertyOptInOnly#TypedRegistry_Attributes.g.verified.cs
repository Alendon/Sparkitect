//HintName: TypedRegistry_Attributes.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591
#nullable enable

namespace DiTest;

public partial class TypedRegistry
{
[global::Sparkitect.Modding.RegistrationMarker("typed_registry")]
    public class RegisterTypedAttribute([global::Sparkitect.Utilities.SnakeCase] string identifier) : global::System.Attribute, global::Sparkitect.Modding.IRegisterMarker
    {
        public bool GroupAtRoot { get; set; } = false;
    }

[global::Sparkitect.Modding.RegistrationMarker("typed_registry")]
    public class RegisterUntypedAttribute([global::Sparkitect.Utilities.SnakeCase] string identifier) : global::System.Attribute, global::Sparkitect.Modding.IRegisterMarker
    {
        public bool GroupAtRoot { get; set; } = false;
    }
}
