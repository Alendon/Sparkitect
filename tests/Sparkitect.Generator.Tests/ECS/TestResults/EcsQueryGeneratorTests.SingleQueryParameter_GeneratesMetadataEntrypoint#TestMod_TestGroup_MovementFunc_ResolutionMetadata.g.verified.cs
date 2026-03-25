//HintName: TestMod_TestGroup_MovementFunc_ResolutionMetadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.CompilerGenerated.DI;

[global::Sparkitect.DI.Resolution.ResolutionMetadataEntrypoint<global::TestMod.TestGroup.MovementFunc>]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class TestMod_TestGroup_MovementFunc_ResolutionMetadata : global::Sparkitect.DI.Resolution.IResolutionMetadataEntrypoint<global::TestMod.TestGroup.MovementFunc>
{
    public void ConfigureResolutionMetadata(global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.List<object>> dependencies)
    {

      {
        dependencies.TryAdd(typeof(global::TestMod.SimpleQuery), new());
        dependencies[typeof(global::TestMod.SimpleQuery)].Add(
            new global::Sparkitect.ECS.Queries.SgQueryMetadata<global::TestMod.SimpleQuery>(
                global::TestMod.SimpleQuery.ReadComponentIds,
                global::TestMod.SimpleQuery.WriteComponentIds,
                world => new global::TestMod.SimpleQuery(world)));
      }

    }
}
