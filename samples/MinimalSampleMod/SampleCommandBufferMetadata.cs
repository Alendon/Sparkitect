using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Commands;

namespace MinimalSampleMod;

[ResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>]
internal class SampleCommandBufferMetadata
    : IResolutionMetadataEntrypoint<MinimalSystemGroup.SampleFunc>
{
    public void ConfigureResolutionMetadata(
        Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(
            new CommandBufferAccessorMetadata(null!));
    }
}
