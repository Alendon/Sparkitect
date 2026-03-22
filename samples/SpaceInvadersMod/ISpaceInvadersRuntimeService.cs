using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace SpaceInvadersMod;

[StateFacade<ISpaceInvadersRuntimeServiceStateFacade>]
public interface ISpaceInvadersRuntimeService
{
    IWorld? GetWorld();
}

[FacadeFor<ISpaceInvadersRuntimeService>]
public interface ISpaceInvadersRuntimeServiceStateFacade
{
    IWorld BuildWorld();
    void SimulateWorld();
    void DestroyWorld();
}
