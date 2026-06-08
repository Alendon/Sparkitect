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

    /// <summary>
    /// The mod-owned window. Exposed so the shared-image registration can read
    /// <c>Window.Swapchain.Extent</c>.
    /// </summary>
    ISparkitWindow Window { get; }

    /// <summary>Whether the window is still open.</summary>
    bool IsOpen { get; }

    RenderEntity[] GetRenderBuffer();

    /// <summary>
    /// Maps the supplied span onto the engine entity element, builds a pooled entity-list resource,
    /// and publishes it through the graph's external-resource door.
    /// </summary>
    void PublishEntities(ReadOnlySpan<RenderEntity> entities);

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

    /// <summary>Creates the mod-owned window before the render-graph registries are processed.</summary>
    void Initialize();

    /// <summary>Builds the render graph after the render-graph registries are processed.</summary>
    void CreateGraph();

    /// <summary>Drives one render-graph frame.</summary>
    void RunFrame();

    /// <summary>Tears down the render graph.</summary>
    void ShutdownGraph();

    /// <summary>Disposes the window.</summary>
    void Cleanup();

    void ProcessInput();
    void CheckGameState();
}
