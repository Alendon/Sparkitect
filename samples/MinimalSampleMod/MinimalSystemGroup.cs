using System.Runtime.CompilerServices;
using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[SystemGroupRegistry.RegisterSystemGroup("minimal")]
public partial class MinimalSystemGroup : IHasIdentification
{
    public static Identification Identification => EcsSystemGroupID.MinimalSampleMod.Minimal;

    [EcsSystemFunction("sample")]
    [EcsSystemScheduling]
    private static unsafe void SampleSystem(IDummyValueManager dummyValueManager)
    {
        //This setup of world access is planned to later be wrapped in coming queries
        var world = dummyValueManager.GetWorld();
        
        
        var storages = world!.Resolve([new InteractionCapability()]);
        foreach (var handle in storages)
        {
            var iteration = world.GetStorage(handle).As<IChunkedIteration>()!;
            ChunkHandle chunkHandle = default;

            while (iteration.GetNextChunk(ref chunkHandle, out var length))
            {
                var components = new Span<MinimalComponent>(
                    iteration.GetChunkComponentData(ref chunkHandle, UnmanagedComponentID.MinimalSampleMod.Minimal),
                    length);

                foreach (ref var component in components)
                {
                    SystemLogic(ref component);
                }
            }
        }
    }

    private static void SystemLogic(ref MinimalComponent component)
    {
        Log.Information("Component Value: {Value}",component.Value);
        component.Value++;
    }

    struct InteractionCapability : ICapabilityRequirement<IChunkedIteration, ComponentSetMetadata>
    {
        public bool Matches(ComponentSetMetadata metadata)
        {
            return metadata.Components.Contains(UnmanagedComponentID.MinimalSampleMod.Minimal);
        }
    }
}