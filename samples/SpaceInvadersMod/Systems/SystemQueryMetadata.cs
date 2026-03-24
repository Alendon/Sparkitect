using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;

namespace SpaceInvadersMod;

// Hand-written metadata entrypoints for ECS system DI parameter resolution (v1.4).
// v1.5 will automate this via the source generator.

// PlayerInputSystem: PlayerInputQuery, ICommandBufferAccessor, FrameTimingHolder
[ResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.PlayerInputFunc>]
internal class PlayerInputFuncMetadata
    : IResolutionMetadataEntrypoint<SpaceInvadersSystemGroup.PlayerInputFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(PlayerInputQuery), new());
        dependencies[typeof(PlayerInputQuery)].Add(
            new SgQueryMetadata<PlayerInputQuery>(
                PlayerInputQuery.ReadComponentIds,
                PlayerInputQuery.WriteComponentIds,
                world => new PlayerInputQuery(world)));

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
            new SgQueryMetadata<BulletQuery>(
                BulletQuery.ReadComponentIds,
                BulletQuery.WriteComponentIds,
                world => new BulletQuery(world)));

        dependencies.TryAdd(typeof(PlayerQuery), new());
        dependencies[typeof(PlayerQuery)].Add(
            new SgQueryMetadata<PlayerQuery>(
                PlayerQuery.ReadComponentIds,
                PlayerQuery.WriteComponentIds,
                world => new PlayerQuery(world)));

        dependencies.TryAdd(typeof(EnemyQuery), new());
        dependencies[typeof(EnemyQuery)].Add(
            new SgQueryMetadata<EnemyQuery>(
                EnemyQuery.ReadComponentIds,
                EnemyQuery.WriteComponentIds,
                world => new EnemyQuery(world)));
    }
}

// EnemyAiSystem: EnemyAiQuery, ICommandBufferAccessor, FrameTimingHolder
[ResolutionMetadataEntrypoint<GameplayGroup.EnemyAiFunc>]
internal class EnemyAiFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.EnemyAiFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(EnemyAiQuery), new());
        dependencies[typeof(EnemyAiQuery)].Add(
            new SgQueryMetadata<EnemyAiQuery>(
                EnemyAiQuery.ReadComponentIds,
                EnemyAiQuery.WriteComponentIds,
                world => new EnemyAiQuery(world)));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));

        dependencies.TryAdd(typeof(FrameTimingHolder), new());
        dependencies[typeof(FrameTimingHolder)].Add(new FrameTimingMetadata());
    }
}

// MovementSystem: MovementQuery, FrameTimingHolder
[ResolutionMetadataEntrypoint<GameplayGroup.MovementFunc>]
internal class MovementFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.MovementFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(MovementQuery), new());
        dependencies[typeof(MovementQuery)].Add(
            new SgQueryMetadata<MovementQuery>(
                MovementQuery.ReadComponentIds,
                MovementQuery.WriteComponentIds,
                world => new MovementQuery(world)));

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
            new SgQueryMetadata<BulletQuery>(
                BulletQuery.ReadComponentIds,
                BulletQuery.WriteComponentIds,
                world => new BulletQuery(world)));

        dependencies.TryAdd(typeof(EnemyQuery), new());
        dependencies[typeof(EnemyQuery)].Add(
            new SgQueryMetadata<EnemyQuery>(
                EnemyQuery.ReadComponentIds,
                EnemyQuery.WriteComponentIds,
                world => new EnemyQuery(world)));

        dependencies.TryAdd(typeof(PlayerQuery), new());
        dependencies[typeof(PlayerQuery)].Add(
            new SgQueryMetadata<PlayerQuery>(
                PlayerQuery.ReadComponentIds,
                PlayerQuery.WriteComponentIds,
                world => new PlayerQuery(world)));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));
    }
}

// BulletCleanupSystem: BulletCleanupQuery, ICommandBufferAccessor
[ResolutionMetadataEntrypoint<GameplayGroup.BulletCleanupFunc>]
internal class BulletCleanupFuncMetadata
    : IResolutionMetadataEntrypoint<GameplayGroup.BulletCleanupFunc>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies.TryAdd(typeof(BulletCleanupQuery), new());
        dependencies[typeof(BulletCleanupQuery)].Add(
            new SgQueryMetadata<BulletCleanupQuery>(
                BulletCleanupQuery.ReadComponentIds,
                BulletCleanupQuery.WriteComponentIds,
                world => new BulletCleanupQuery(world)));

        dependencies.TryAdd(typeof(ICommandBufferAccessor), new());
        dependencies[typeof(ICommandBufferAccessor)].Add(new CommandBufferAccessorMetadata(null!));
    }
}
