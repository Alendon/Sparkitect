using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public class EntryStateSelectorEntrypointAttribute : Attribute;

public interface IEntryStateSelector : IConfigurationEntrypoint<EntryStateSelectorEntrypointAttribute>
{
    public Identification SelectEntryState(ICoreContainer container);
}

