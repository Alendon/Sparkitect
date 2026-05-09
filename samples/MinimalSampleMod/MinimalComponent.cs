using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[UnmanagedComponentRegistry.RegisterComponent("minimal")]
public partial struct MinimalComponent : IHasIdentification
{
    public int Value;
}