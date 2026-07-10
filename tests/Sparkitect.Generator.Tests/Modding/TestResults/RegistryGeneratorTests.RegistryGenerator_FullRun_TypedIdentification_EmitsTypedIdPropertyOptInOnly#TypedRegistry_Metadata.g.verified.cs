//HintName: TypedRegistry_Metadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

[assembly: global::Sparkitect.Modding.RegistryMetadataAttribute<global::SampleTest.Generated.TypedRegistry_Metadata>]

namespace SampleTest.Generated;


[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
public class TypedRegistry_Metadata
{
    public const string TypeName = "TypedRegistry";
    public const string Key = "typed_registry";
    public const string ContainingNamespace = "DiTest";
    public const bool IsExternal = false;
    public const string RegisterMethods = "RegisterTyped;RegisterUntyped";
    public const string ResourceFiles = "";
    public const string OwningModule = "global::Sparkitect.Modding.TestModule";
    public const string AliasSuffix = "";


    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class RegisterTyped
    {
        public const string FunctionName = "RegisterTyped";
        public const int PrimaryParameterKind = 4;
        public const int Constraint = 1;
        public const string TypeConstraint = "Sparkitect.Modding.IHasIdentification";
        public const string KeyedFactoryMarkerTBase = "";
        public const string KeyedFactoryMarkerTKey = "Sparkitect.Modding.Identification";
        public const string TypedIdentificationTypeParameterName = "TPayload";
        public const string TypeParameterNames = "TPayload";
        public const string ConstraintRefs = "";
        public const string ValueParameterGeneric = "";
        public const string CrossRegistryMarkers = "";
    }


    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class RegisterUntyped
    {
        public const string FunctionName = "RegisterUntyped";
        public const int PrimaryParameterKind = 4;
        public const int Constraint = 1;
        public const string TypeConstraint = "Sparkitect.Modding.IHasIdentification";
        public const string KeyedFactoryMarkerTBase = "";
        public const string KeyedFactoryMarkerTKey = "Sparkitect.Modding.Identification";
        public const string TypedIdentificationTypeParameterName = "";
        public const string TypeParameterNames = "TOther";
        public const string ConstraintRefs = "";
        public const string ValueParameterGeneric = "";
        public const string CrossRegistryMarkers = "";
    }


}
