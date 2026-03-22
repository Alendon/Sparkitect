using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Queries;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[ResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>]
internal class SampleSystemQueryMetadata
    : IResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>
{
    public void ConfigureResolutionMetadata(
        Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ComponentQuery), new());
        dependencies[typeof(ComponentQuery)].Add(
            new ComponentQueryMetadata([
                UnmanagedComponentID.MinimalSampleMod.Minimal
            ]));
    }
}
