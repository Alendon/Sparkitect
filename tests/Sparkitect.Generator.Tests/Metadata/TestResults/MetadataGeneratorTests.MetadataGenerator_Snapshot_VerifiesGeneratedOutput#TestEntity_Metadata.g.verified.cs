//HintName: TestEntity_Metadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.Metadata.ApplyMetadataEntrypointAttribute<global::MetadataTest.TestMetadataType>]
internal class TestEntity_Metadata
    : global::Sparkitect.Metadata.ApplyMetadataEntrypoint<global::MetadataTest.TestMetadataType>
{
    public override void CollectMetadata(
        global::System.Collections.Generic.Dictionary<global::Sparkitect.Modding.Identification, global::MetadataTest.TestMetadataType> metadata)
    {
        {

            var instance = new global::MetadataTest.TestMetadataType(

            );
            metadata[global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestEntity>()] = instance;
        }
    }
}
