//HintName: TypedProviderRegistry_Metadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

[assembly: global::Sparkitect.Modding.RegistryMetadataAttribute<global::SampleTest.Generated.TypedProviderRegistry_Metadata>]

namespace SampleTest.Generated;


[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
public class TypedProviderRegistry_Metadata
{
    public const string TypeName = "TypedProviderRegistry";
    public const string Key = "typed_provider_registry";
    public const string ContainingNamespace = "DiTest";
    public const bool IsExternal = false;
    public const string RegisterMethods = "RegisterTypedProvider";
    public const string ResourceFiles = "";
    public const string OwningModule = "global::Sparkitect.Modding.TestModule";
    public const string AliasSuffix = "";


    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class RegisterTypedProvider
    {
        public const string FunctionName = "RegisterTypedProvider";
        public const int PrimaryParameterKind = 4;
        public const int Constraint = 1;
        public const string TypeConstraint = "DiTest.IValueProvider;Sparkitect.Modding.IHasIdentification";
        public const string KeyedFactoryMarkerTBase = "";
        public const string KeyedFactoryMarkerTKey = "Sparkitect.Modding.Identification";
        public const string TypedIdentificationTypeParameterName = "TPayload";
        public const string TypeParameterNames = "TPayload";
        public const string ConstraintRefs = "";
        public const string ValueParameterGeneric = "";
        public const string CrossRegistryMarkers = "";
    }


}
