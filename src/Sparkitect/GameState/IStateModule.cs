using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IStateModule
{
    public static abstract IReadOnlyList<Type> UsedServices { get; }
    public static abstract Identification Identification { get; }
}