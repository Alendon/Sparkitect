using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[SystemGroupRegistry.RegisterSystemGroup("minimal")]
[SystemGroupScheduling]
public partial class MinimalSystemGroup : IHasIdentification
{
    public static Identification Identification => EcsSystemGroupID.MinimalSampleMod.Minimal;

    [EcsSystemFunction("sample")]
    [EcsSystemScheduling]
    private static void SampleSystem(ComponentQuery query)
    {
        foreach (var entity in query)
        {
            ref var component = ref entity.GetRef<MinimalComponent>();
            Log.Information("Component Value: {Value}",component.Value);
            component.Value++;
        }
    }
}
