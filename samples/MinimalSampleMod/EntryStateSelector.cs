using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.DI.Container;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[EntryStateSelectorEntrypoint]
public class EntryStateSelector : IEntryStateSelector
{
    public Identification SelectEntryState(ICoreContainer container)
    {
        return StateID.MinimalSampleMod.Sample;
    }
}