using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.States;

//The entry game state represents the first state which is loaded after the root state is finished.
//It can be seen as the main function for the "main" mod.
//This Mod has to actively mark their EntryState Implementation.
//As the Root State is very minimal, the Entry State is intended to do a first "selection" of what actually happens in this run
//EG do we have graphics? Or is the --server options specified?. By this  we have to select the next actual content game state.
//Therefor this state also does not have a lot to actually implement later
//Or for other simpler games, this can be directly the game loop. And the associated transitions directly initialize the game.
//This freedom is fully given to the developer.
//Sparkitect will over time add more PreBuilt State Modules, which can be used by  developers to easily configure their game.
[StateDescriptionRegistry.RegisterStateAbc("entry")]
public class EntryState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static IReadOnlyList<Identification> Modules => [];
}