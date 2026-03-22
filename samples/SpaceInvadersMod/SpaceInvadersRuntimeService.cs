using System.Diagnostics;
using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

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

    public IWorld? GetWorld() => _world;

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

        _world.SetSystemTree(systemManager.BuildTree(EcsSystemGroupID.SpaceInvadersMod.SpaceInvaders));

        // Spawn player entity
        var playerAccessor = _world.GetStorage(playerHandle);
        var playerSlot = playerAccessor.AsStorage<int>()!.AllocateEntity();
        var playerComponents = playerAccessor.As<IComponentAccess<int>>()!;
        playerComponents.Set(posId, playerSlot, new Position { Value = new Vector2(0.5f, 0.9f) });
        playerComponents.Set(velId, playerSlot, new Velocity { Value = Vector2.Zero });
        playerComponents.Set(cooldownId, playerSlot, new ShootCooldown { Remaining = 0f });
        playerComponents.Set(playerTagId, playerSlot, new PlayerTag());

        // Spawn enemy formation: 5 rows x 11 cols
        var enemyAccessor = _world.GetStorage(enemyHandle);
        var enemySlotAllocator = enemyAccessor.AsStorage<int>()!;
        var enemyComponents = enemyAccessor.As<IComponentAccess<int>>()!;

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
                enemyComponents.Set(posId, slot, new Position { Value = new Vector2(startX + col * spacingX, startY + row * spacingY) });
                enemyComponents.Set(velId, slot, new Velocity { Value = Vector2.Zero });
                enemyComponents.Set(cooldownId, slot, new ShootCooldown { Remaining = 0f });
                enemyComponents.Set(enemyTagId, slot, new EnemyTag());
            }
        }

        systemManager.NotifyRebuild(_world);

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
