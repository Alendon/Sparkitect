using Serilog;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[SystemGroupRegistry.RegisterSystemGroup("minimal")]
[SystemGroupScheduling]
public partial class MinimalSystemGroup : IHasIdentification
{
    [EcsSystemFunction("sample")]
    [EcsSystemScheduling]
    private static void SampleSystem(SampleQuery query)
    {
        foreach (var entity in query)
        {
            ref var component = ref entity.GetMinimalComponent();
            int componentValue = component.Value;
            Log.Information("Component Value: {Value}", componentValue);
            component.Value++;
        }
    }
}
