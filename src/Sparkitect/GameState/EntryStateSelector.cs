using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public class EntryStateSelectorEntrypointAttribute : Attribute;

public abstract class EntryStateSelector : ConfigurationEntrypoint<EntryStateSelectorEntrypointAttribute>
{
    public abstract Identification SelectEntryState(ICoreContainer container);
}

