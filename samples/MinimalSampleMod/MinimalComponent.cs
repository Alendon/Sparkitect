using Sparkitect.ECS.Components;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[UnmanagedComponentRegistry.RegisterComponent("minimal")]
public partial struct MinimalComponent : IHasIdentification
{
    public int Value;
}