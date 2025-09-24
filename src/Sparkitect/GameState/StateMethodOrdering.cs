using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

public class IStateMethodOrderingEntrypointAttribute : Attribute;

public abstract class StateMethodOrdering : IConfigurationEntrypoint<IStateMethodOrderingEntrypointAttribute>
{
    public abstract void ConfigureOrdering(HashSet<OrderingEntry> ordering);
}

public record OrderingEntry((Identification Parent, string Method) Before, (Identification Parent, string Method) After);