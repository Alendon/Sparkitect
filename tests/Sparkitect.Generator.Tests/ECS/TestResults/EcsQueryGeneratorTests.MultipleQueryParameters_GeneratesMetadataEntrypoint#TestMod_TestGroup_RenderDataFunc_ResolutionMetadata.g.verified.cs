//HintName: TestMod_TestGroup_RenderDataFunc_ResolutionMetadata.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.CompilerGenerated.DI;

[global::Sparkitect.DI.Resolution.ResolutionMetadataEntrypoint<global::TestMod.TestGroup.RenderDataFunc>]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
internal class TestMod_TestGroup_RenderDataFunc_ResolutionMetadata : global::Sparkitect.DI.Resolution.IResolutionMetadataEntrypoint<global::TestMod.TestGroup.RenderDataFunc>
{
    public void ConfigureResolutionMetadata(global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.List<object>> dependencies)
    {

      {
        dependencies.TryAdd(typeof(global::TestMod.QueryA), new());
        dependencies[typeof(global::TestMod.QueryA)].Add(
            new global::Sparkitect.ECS.Queries.SgQueryMetadata<global::TestMod.QueryA>(
                global::TestMod.QueryA.ReadComponentIds,
                global::TestMod.QueryA.WriteComponentIds,
                world => new global::TestMod.QueryA(world)));
      }

      {
        dependencies.TryAdd(typeof(global::TestMod.QueryB), new());
        dependencies[typeof(global::TestMod.QueryB)].Add(
            new global::Sparkitect.ECS.Queries.SgQueryMetadata<global::TestMod.QueryB>(
                global::TestMod.QueryB.ReadComponentIds,
                global::TestMod.QueryB.WriteComponentIds,
                world => new global::TestMod.QueryB(world)));
      }

      {
        dependencies.TryAdd(typeof(global::TestMod.QueryC), new());
        dependencies[typeof(global::TestMod.QueryC)].Add(
            new global::Sparkitect.ECS.Queries.SgQueryMetadata<global::TestMod.QueryC>(
                global::TestMod.QueryC.ReadComponentIds,
                global::TestMod.QueryC.WriteComponentIds,
                world => new global::TestMod.QueryC(world)));
      }

    }
}
