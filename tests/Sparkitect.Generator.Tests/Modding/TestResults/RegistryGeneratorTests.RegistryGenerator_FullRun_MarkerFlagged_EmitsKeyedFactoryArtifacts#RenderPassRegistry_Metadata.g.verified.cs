//HintName: RenderPassRegistry_Metadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

[assembly: global::Sparkitect.Modding.RegistryMetadataAttribute<global::SampleTest.Generated.RenderPassRegistry_Metadata>]

namespace SampleTest.Generated;


[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
public class RenderPassRegistry_Metadata
{
    public const string TypeName = "RenderPassRegistry";
    public const string Key = "render_pass";
    public const string ContainingNamespace = "DiTest";
    public const bool IsExternal = false;
    public const string RegisterMethods = "RegisterRenderPass";
    public const string ResourceFiles = "";
    public const string OwningModule = "global::Sparkitect.Modding.TestModule";


    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class RegisterRenderPass
    {
        public const string FunctionName = "RegisterRenderPass";
        public const int PrimaryParameterKind = 4;
        public const int Constraint = 1;
        public const string TypeConstraint = "DiTest.IRenderPass;Sparkitect.Modding.IHasIdentification";
        public const string KeyedFactoryMarkerTBase = "global::DiTest.IRenderPass";
        public const string KeyedFactoryMarkerTKey = "Sparkitect.Modding.Identification";
    }


}
