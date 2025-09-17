using Sparkitect.Modding;

namespace Sparkitect.GameState.Samples.States;

[StateRegistry.RegisterState("desktop")]
public class Desktop : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty;
}