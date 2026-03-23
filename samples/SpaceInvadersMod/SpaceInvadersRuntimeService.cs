using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
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
    private int _renderEntityCount;
    private bool _isGameplayActive;
    private ISparkitWindow? _window;
    private StorageHandle _playerStorageHandle;
    private StorageHandle _enemyStorageHandle;

    // Input: physical key → action mapping, cached per frame
    private static readonly Dictionary<Key, GameAction> KeyMap = new()
    {
        [Key.Left] = GameAction.MoveLeft,
        [Key.A] = GameAction.MoveLeft,
        [Key.Right] = GameAction.MoveRight,
        [Key.D] = GameAction.MoveRight,
        [Key.Up] = GameAction.Shoot,
        [Key.Space] = GameAction.TogglePause,
    };

    private readonly HashSet<GameAction> _activeActions = [];
    private bool _pauseToggleConsumed;

    public required IWindowManager WindowManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required IShaderManager ShaderManager { private get; init; }

    public IWorld? GetWorld() => _world;
    public RenderEntity[] GetRenderBuffer() => _renderBuffer;
    public void SetRenderEntityCount(int count) => _renderEntityCount = count;
    public int GetRenderEntityCount() => _renderEntityCount;
    public bool IsGameplayActive => _isGameplayActive;
    public void SetGameplayActive(bool active) => _isGameplayActive = active;

    public bool IsActionDown(GameAction action) => _activeActions.Contains(action);

    public void ProcessInput()
    {
        _activeActions.Clear();

        var keyboard = _window?.Keyboard;
        if (keyboard is null) return;

        foreach (var (key, action) in KeyMap)
        {
            if (keyboard.IsKeyDown(key))
                _activeActions.Add(action);
        }

        // Edge-detect pause toggle: only fire once per press
        if (_activeActions.Contains(GameAction.TogglePause))
        {
            if (_pauseToggleConsumed)
                _activeActions.Remove(GameAction.TogglePause);
            else
                _pauseToggleConsumed = true;
        }
        else
        {
            _pauseToggleConsumed = false;
        }
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
                _isGameplayActive = false;
                _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
            }
        }
        else if (IsActionDown(GameAction.TogglePause))
        {
            _isGameplayActive = true;
            _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
        }
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
                enemyComponents.Set(slot, new ShootCooldown { Remaining = 0f });
                enemyComponents.Set(slot, new EnemyTag());
            }
        }

        systemManager.NotifyRebuild(_world);

        // Per D-11: Game starts with GameplayGroup inactive (paused)
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

    public void InitializeRendering() { /* Plan 03 */ }
    public void Render() { /* Plan 03 */ }
    public void CleanupRendering() { /* Plan 03 */ }
}
