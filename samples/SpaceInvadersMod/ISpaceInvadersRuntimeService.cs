using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace SpaceInvadersMod;

[StateFacade<ISpaceInvadersRuntimeServiceStateFacade>]
public interface ISpaceInvadersRuntimeService
{
    IWorld? GetWorld();
    RenderEntity[] GetRenderBuffer();
    void SetRenderEntityCount(int count);
    int GetRenderEntityCount();
    bool IsGameplayActive { get; }
    void SetGameplayActive(bool active);
    bool IsActionDown(GameAction action);
}

[FacadeFor<ISpaceInvadersRuntimeService>]
public interface ISpaceInvadersRuntimeServiceStateFacade
{
    IWorld BuildWorld();
    void SimulateWorld();
    void DestroyWorld();
    void InitializeRendering();
    void Render();
    void CleanupRendering();
    void ProcessInput();
    void CheckGameState();
}
