//HintName: MovementFunc_ResourceAccess.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591
#nullable enable

namespace TestMod.Generated;

[global::Sparkitect.Metadata.ApplyMetadataEntrypointAttribute<global::Sparkitect.ECS.Systems.EcsSystemResourceAccess>]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class MovementFunc_ResourceAccess
    : global::Sparkitect.Metadata.ApplyMetadataEntrypoint<global::Sparkitect.ECS.Systems.EcsSystemResourceAccess>
{
    public override void CollectMetadata(
        global::System.Collections.Generic.Dictionary<global::Sparkitect.Modding.Identification, global::Sparkitect.ECS.Systems.EcsSystemResourceAccess> metadata)
    {
        var reads = new global::System.Collections.Generic.HashSet<global::Sparkitect.Modding.Identification>();

        reads.UnionWith(global::TestMod.MoveQuery.ReadComponentIds);


        var writes = new global::System.Collections.Generic.HashSet<global::Sparkitect.Modding.Identification>();

        writes.UnionWith(global::TestMod.MoveQuery.WriteComponentIds);


        var id = global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestGroup.MovementFunc>();
        if (id != default)
            metadata[id] = new global::Sparkitect.ECS.Systems.EcsSystemResourceAccess(reads, writes);
    }
}
