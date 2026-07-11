using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Serilog;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Push;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;
using Sparkitect.Windowing;

namespace SpaceInvadersMod;

[StateService<ISpaceInvadersRuntimeService, SpaceInvadersModule>]
public class SpaceInvadersRuntimeService(IComponentManager componentManager, ISystemManager systemManager)
    : ISpaceInvadersRuntimeService, ISpaceInvadersRuntimeServiceStateFacade
{
    private IWorld? _world;
    private IObjectTracker<IDisposable>? _tracker;
    private ICommandBufferAccessor? _commandBufferAccessor;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    private RenderEntity[] _renderBuffer = new RenderEntity[SpaceInvadersConstants.MaxRenderEntities];
    private bool _isGameplayActive;
    private ISparkitWindow? _window;
    private RenderGraph? _renderGraph;
    private StorageHandle _playerStorageHandle;
    private StorageHandle _enemyStorageHandle;

    private IPushBinding? _movePush;
    private IPushBinding? _shootPush;
    private IPullBinding<bool>? _pausePull;
    private IPullBinding<bool>? _restartPull;
    private float _moveIntent;
    private bool _shootHeld;
    private ActionResult<bool> _pausePrevious = ActionResult<bool>.NoValue;
    private ActionResult<bool> _restartPrevious = ActionResult<bool>.NoValue;
    private bool _pauseRequested;
    private bool _restartRequested;

    public required IWindowManager WindowManager { private get; init; }
    public required IRenderGraphManager RenderGraphManager { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required IInputActions InputActions { private get; init; }

    public IWorld? GetWorld() => _world;

    public ISparkitWindow Window =>
        _window ?? throw new InvalidOperationException(
            "SpaceInvadersRuntimeService.Window: the window has not been created yet.");

    public bool IsOpen => _window?.IsOpen ?? false;

    public RenderEntity[] GetRenderBuffer() => _renderBuffer;
    public bool IsGameplayActive => _isGameplayActive;
    public void SetGameplayActive(bool active) => _isGameplayActive = active;

    public float MoveAxis => _moveIntent;
    public bool IsShootHeld => _shootHeld;

    /// <summary>Creates the mod-owned window before the render-graph registries are processed.</summary>
    public void Initialize()
    {
        _window = WindowManager.CreateGameWindow("Space Invaders");
        Log.Debug("Space Invaders runtime initialized");
    }

    /// <summary>Builds the three-pass staging → compute → copy graph and sets frame pacing.</summary>
    public void CreateGraph()
    {
        if (_renderGraph is not null) return;

        _renderGraph = RenderGraphManager.CreateGraph<RenderGraph>(
            new List<Identification>
            {
                RenderPassID.SpaceInvadersMod.SpaceInvadersStaging,
                RenderPassID.SpaceInvadersMod.SpaceInvadersCompute,
                RenderPassID.SpaceInvadersMod.SpaceInvadersCopy,
            },
            _window!);
    }

    public void WireInput()
    {
        _movePush = ActionID.SpaceInvadersMod.MoveHorizontal.Push(InputActions, v => _moveIntent = v);
        _shootPush = ActionID.SpaceInvadersMod.Shoot.Push(InputActions, v => _shootHeld = v);
        _pausePull = ActionID.SpaceInvadersMod.TogglePause.Pull(InputActions);
        _restartPull = ActionID.SpaceInvadersMod.Restart.Pull(InputActions);
    }

    /// <summary>Drives one render-graph frame (acquire/submit/present owned by the graph).</summary>
    public void RunFrame() => _renderGraph?.RunFrame();

    /// <summary>
    /// Maps the supplied <see cref="RenderEntity"/> span onto the layout-compatible
    /// <see cref="GpuRenderEntity"/> and publishes it through the graph's external-push door, keyed by the
    /// <c>entities_raw</c> moment. The graph swap-copies the span into its own snapshot, so the caller's
    /// reusable buffer is free to mutate immediately.
    /// </summary>
    public void PublishEntities(ReadOnlySpan<RenderEntity> entities)
    {
        if (_renderGraph is null) return;

        var mapped = MemoryMarshal.Cast<RenderEntity, GpuRenderEntity>(entities);
        _renderGraph.GetHandler<IExternalPushHandler>()!.Publish(GraphMomentID.SpaceInvadersMod.EntitiesRaw, mapped);
    }

    public void ShutdownGraph()
    {
        _renderGraph?.Dispose();
        _renderGraph = null;
    }

    public void Cleanup()
    {
        _movePush?.Dispose();
        _movePush = null;
        _shootPush?.Dispose();
        _shootPush = null;
        _pausePull?.Dispose();
        _pausePull = null;
        _restartPull?.Dispose();
        _restartPull = null;

        if (_window is not null)
            WindowManager.DestroyWindow(_window);
        _window = null;
        Log.Debug("Space Invaders rendering cleanup complete");
    }

    // Runs after this frame's input processing (push callbacks already re-asserted _moveIntent/
    // _shootHeld): derives the once-per-press pause/restart edges from the NoValue-preserving
    // pull stream. The held-intent fields reset at the end of CheckGameState, after every
    // consumer has read them.
    public void ProcessInput()
    {
        var pause = _pausePull!.Read();
        _pauseRequested = ActionEdge.IsPressEdge(_pausePrevious, pause);
        _pausePrevious = pause;

        var restart = _restartPull!.Read();
        _restartRequested = ActionEdge.IsPressEdge(_restartPrevious, restart);
        _restartPrevious = restart;
    }

    public void CheckGameState()
    {
        if (_world is null) return;

        if (_isGameplayActive)
        {
            var playerCount = _world.GetStorage(_playerStorageHandle).Count;
            var enemyCount = _world.GetStorage(_enemyStorageHandle).Count;

            if (playerCount == 0 || enemyCount == 0)
            {
                // Auto-reset: rebuild world immediately on death/victory
                DestroyWorld();
                BuildWorld();
                // Start paused so player can see fresh board before pressing Space
            }
            else if (_pauseRequested)
            {
                _isGameplayActive = false;
                _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
            }
        }
        else if (_restartRequested)
        {
            // Rebuild the world to restart the game
            DestroyWorld();
            BuildWorld();
            _isGameplayActive = true;
            _world!.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
        }
        else if (_pauseRequested || _shootHeld)
        {
            _isGameplayActive = true;
            _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
        }

        // All consumers (PlayerInputSystem during simulate, the checks above) have read this
        // frame's intent; reset so a released key does not persist into the next frame.
        _pauseRequested = false;
        _restartRequested = false;
        _moveIntent = 0f;
        _shootHeld = false;
    }

    public IWorld BuildWorld()
    {
        _world = IWorld.Create();
        _tracker = new ObjectTracker<IDisposable>();
        _frameTimer.Restart();
        _lastFrameTime = 0f;

        (Identification, int) Meta(Identification id) => (id, componentManager.GetSize(id));

        var posId = UnmanagedComponentID.SpaceInvadersMod.Position;
        var velId = UnmanagedComponentID.SpaceInvadersMod.Velocity;
        var cooldownId = UnmanagedComponentID.SpaceInvadersMod.ShootCooldown;
        var playerTagId = UnmanagedComponentID.SpaceInvadersMod.PlayerTag;
        var enemyTagId = UnmanagedComponentID.SpaceInvadersMod.EnemyTag;
        var bulletDataId = UnmanagedComponentID.SpaceInvadersMod.BulletData;

        // Player archetype: Position, Velocity, ShootCooldown, PlayerTag
        var playerStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(cooldownId), Meta(playerTagId)],
            _tracker, _world, initialCapacity: 4);

        // Enemy archetype: Position, Velocity, ShootCooldown, EnemyTag
        var enemyStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(cooldownId), Meta(enemyTagId)],
            _tracker, _world, initialCapacity: 64);

        // Bullet archetype: Position, Velocity, BulletData
        var bulletStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(bulletDataId)],
            _tracker, _world, initialCapacity: 32);

        var playerHandle = _world.AddStorage(playerStorage, playerStorage.CreateCapabilityRegistrations());
        var enemyHandle = _world.AddStorage(enemyStorage, enemyStorage.CreateCapabilityRegistrations());
        var bulletHandle = _world.AddStorage(bulletStorage, bulletStorage.CreateCapabilityRegistrations());

        playerStorage.SetHandle(playerHandle);
        enemyStorage.SetHandle(enemyHandle);
        bulletStorage.SetHandle(bulletHandle);

        _playerStorageHandle = playerHandle;
        _enemyStorageHandle = enemyHandle;

        _world.SetSystemTree(systemManager.BuildTree(EcsSystemGroupID.SpaceInvadersMod.SpaceInvaders));

        // Spawn player entity
        var playerAccessor = _world.GetStorage(playerHandle);
        var playerSlot = playerAccessor.AsStorage<int>()!.AllocateEntity();
        var playerComponents = playerAccessor.As<IComponentAccess<int>>()!;
        var playerIdAccessor = playerAccessor.As<IEntityIdentity<int>>()!;
        playerIdAccessor.Assign(_world.AllocateEntityId(), playerSlot);
        playerComponents.Set(playerSlot, new Position { Value = new Vector2(0.5f, 0.9f) });
        playerComponents.Set(playerSlot, new Velocity { Value = Vector2.Zero });
        playerComponents.Set(playerSlot, new ShootCooldown { Remaining = 0f });
        playerComponents.Set(playerSlot, new PlayerTag());

        // Spawn enemy formation: 5 rows x 11 cols
        var enemyAccessor = _world.GetStorage(enemyHandle);
        var enemySlotAllocator = enemyAccessor.AsStorage<int>()!;
        var enemyComponents = enemyAccessor.As<IComponentAccess<int>>()!;
        var enemyIdAccessor = enemyAccessor.As<IEntityIdentity<int>>()!;

        const int rows = 5;
        const int cols = 11;
        const float startX = 0.15f;
        const float startY = 0.1f;
        const float spacingX = 0.07f;
        const float spacingY = 0.06f;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var slot = enemySlotAllocator.AllocateEntity();
                enemyIdAccessor.Assign(_world.AllocateEntityId(), slot);
                enemyComponents.Set(slot, new Position { Value = new Vector2(startX + col * spacingX, startY + row * spacingY) });
                enemyComponents.Set(slot, new Velocity { Value = Vector2.Zero });
                enemyComponents.Set(slot, new ShootCooldown { Remaining = SpaceInvadersConstants.EnemyShootCooldownMin +
                    Random.Shared.NextSingle() * (SpaceInvadersConstants.EnemyShootCooldownMax - SpaceInvadersConstants.EnemyShootCooldownMin) });
                enemyComponents.Set(slot, new EnemyTag());
            }
        }

        systemManager.NotifyRebuild(_world);

        // Game starts with the gameplay group inactive (paused).
        _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
        _isGameplayActive = false;

        return _world;
    }

    public void SimulateWorld()
    {
        if (_world is null) return;

        var currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        systemManager.ExecuteSystems(_world, new FrameTiming(deltaTime, currentTime));

        _commandBufferAccessor ??= systemManager.GetCommandBufferAccessor(_world);
        _commandBufferAccessor?.Playback();
    }

    public void DestroyWorld()
    {
        _commandBufferAccessor = null;
        _frameTimer.Stop();
        _world?.Dispose();
        _world = null;
    }
}
