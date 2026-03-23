using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod;

// Hand-written metadata entrypoints for ECS system DI parameter resolution (v1.4).
// v1.5 will automate this via the source generator.

// PlayerInputSystem: ComponentQuery<EntityId>[PlayerTag], ICommandBufferAccessor, FrameTimingHolder
[ResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.PlayerInputFunc>]
internal class PlayerInputFuncMetadata
    : IResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.PlayerInputFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ComponentQuery<EntityId>), new());
        dependencies[typeof(ComponentQuery<EntityId>)].Add(
            new ComponentQueryMetadata<EntityId>([
                UnmanagedComponentID.SpaceInvadersMod.Position,
                UnmanagedComponentID.SpaceInvadersMod.Velocity,
                UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                UnmanagedComponentID.SpaceInvadersMod.PlayerTag
            ]));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));

        dependencies.TryAdd(typeof(FrameTimingHolder), new());
        dependencies[typeof(FrameTimingHolder)].Add(new FrameTimingMetadata());
    }
}

// RenderDataCollectionSystem: BulletQuery, PlayerQuery, EnemyQuery
[ResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.RenderDataFunc>]
internal class RenderDataFuncMetadata
    : IResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.RenderDataFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(BulletQuery), new());
        dependencies[typeof(BulletQuery)].Add(
            new ComponentQueryMetadata<BulletQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.BulletData
                ],
                static (world, ids, storages, filter) => new BulletQuery(world, ids, storages, filter)));

        dependencies.TryAdd(typeof(PlayerQuery), new());
        dependencies[typeof(PlayerQuery)].Add(
            new ComponentQueryMetadata<PlayerQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                    UnmanagedComponentID.SpaceInvadersMod.PlayerTag
                ],
                static (world, ids, storages, filter) => new PlayerQuery(world, ids, storages, filter)));

        dependencies.TryAdd(typeof(EnemyQuery), new());
        dependencies[typeof(EnemyQuery)].Add(
            new ComponentQueryMetadata<EnemyQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                    UnmanagedComponentID.SpaceInvadersMod.EnemyTag
                ],
                static (world, ids, storages, filter) => new EnemyQuery(world, ids, storages, filter)));
    }
}

// EnemyAiSystem: ComponentQuery<EntityId>[EnemyTag], ICommandBufferAccessor, FrameTimingHolder
[ResolutionMetadataEntrypoint<GameplayGroup.EnemyAiFunc>]
internal class EnemyAiFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.EnemyAiFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ComponentQuery<EntityId>), new());
        dependencies[typeof(ComponentQuery<EntityId>)].Add(
            new ComponentQueryMetadata<EntityId>([
                UnmanagedComponentID.SpaceInvadersMod.Position,
                UnmanagedComponentID.SpaceInvadersMod.Velocity,
                UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                UnmanagedComponentID.SpaceInvadersMod.EnemyTag
            ]));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));

        dependencies.TryAdd(typeof(FrameTimingHolder), new());
        dependencies[typeof(FrameTimingHolder)].Add(new FrameTimingMetadata());
    }
}

// MovementSystem: ComponentQuery[Position+Velocity], FrameTimingHolder
[ResolutionMetadataEntrypoint<GameplayGroup.MovementFunc>]
internal class MovementFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.MovementFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ComponentQuery), new());
        dependencies[typeof(ComponentQuery)].Add(
            new ComponentQueryMetadata([
                UnmanagedComponentID.SpaceInvadersMod.Position,
                UnmanagedComponentID.SpaceInvadersMod.Velocity
            ]));

        dependencies.TryAdd(typeof(FrameTimingHolder), new());
        dependencies[typeof(FrameTimingHolder)].Add(new FrameTimingMetadata());
    }
}

// CollisionSystem: BulletQuery, EnemyQuery, PlayerQuery, ICommandBufferAccessor
[ResolutionMetadataEntrypoint<GameplayGroup.CollisionFunc>]
internal class CollisionFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.CollisionFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(BulletQuery), new());
        dependencies[typeof(BulletQuery)].Add(
            new ComponentQueryMetadata<BulletQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.BulletData
                ],
                static (world, ids, storages, filter) => new BulletQuery(world, ids, storages, filter)));

        dependencies.TryAdd(typeof(EnemyQuery), new());
        dependencies[typeof(EnemyQuery)].Add(
            new ComponentQueryMetadata<EnemyQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                    UnmanagedComponentID.SpaceInvadersMod.EnemyTag
                ],
                static (world, ids, storages, filter) => new EnemyQuery(world, ids, storages, filter)));

        dependencies.TryAdd(typeof(PlayerQuery), new());
        dependencies[typeof(PlayerQuery)].Add(
            new ComponentQueryMetadata<PlayerQuery, EntityId>(
                [
                    UnmanagedComponentID.SpaceInvadersMod.Position,
                    UnmanagedComponentID.SpaceInvadersMod.Velocity,
                    UnmanagedComponentID.SpaceInvadersMod.ShootCooldown,
                    UnmanagedComponentID.SpaceInvadersMod.PlayerTag
                ],
                static (world, ids, storages, filter) => new PlayerQuery(world, ids, storages, filter)));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));
    }
}

// BulletCleanupSystem: ComponentQuery<EntityId>[BulletData], ICommandBufferAccessor
[ResolutionMetadataEntrypoint<GameplayGroup.BulletCleanupFunc>]
internal class BulletCleanupFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.BulletCleanupFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(ComponentQuery<EntityId>), new());
        dependencies[typeof(ComponentQuery<EntityId>)].Add(
            new ComponentQueryMetadata<EntityId>([
                UnmanagedComponentID.SpaceInvadersMod.Position,
                UnmanagedComponentID.SpaceInvadersMod.Velocity,
                UnmanagedComponentID.SpaceInvadersMod.BulletData
            ]));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));
    }
}
