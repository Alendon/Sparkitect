namespace Sparkitect.GameState;

public interface IStateModule
{
    public static abstract IReadOnlyList<Type> UsedServices { get; }
}