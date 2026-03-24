using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
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
    private static void SampleSystem(SampleQuery query)
    {
        foreach (var entity in query)
        {
            ref var component = ref entity.GetMinimalComponent();
            Log.Information("Component Value: {Value}", component.Value);
            component.Value++;
        }
    }
}
