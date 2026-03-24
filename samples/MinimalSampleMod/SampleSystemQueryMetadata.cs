using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Queries;

namespace MinimalSampleMod;

[ResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>]
internal class SampleSystemQueryMetadata
    : IResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>
{
    public void ConfigureResolutionMetadata(
        Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(SampleQuery), new());
        dependencies[typeof(SampleQuery)].Add(
            new SgQueryMetadata<SampleQuery>(
                SampleQuery.ReadComponentIds,
                SampleQuery.WriteComponentIds,
                world => new SampleQuery(world)));
    }
}
