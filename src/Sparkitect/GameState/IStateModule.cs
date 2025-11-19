using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IStateModule
{
    public static abstract Span<Identification> RequiredModules { get; }
    public static abstract Identification Identification { get; }
}