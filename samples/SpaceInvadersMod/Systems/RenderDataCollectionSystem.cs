using System.Numerics;
using System.Runtime.CompilerServices;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class SpaceInvadersSystemGroup
{
    [EcsSystemFunction("render_data")]
    [EcsSystemScheduling]
    [OrderAfter<GameplayGroup>]
    private static unsafe void RenderDataCollectionSystem(
        ISpaceInvadersRuntimeService runtimeService,
        BulletQuery bulletQuery, PlayerQuery playerQuery, EnemyQuery enemyQuery)
    {
        var buffer = runtimeService.GetRenderBuffer();
        int count = 0;

        // Collect players
        count = CollectRenderEntities(playerQuery, buffer, count, SpaceInvadersConstants.TypePlayer);

        // Collect enemies
        count = CollectRenderEntities(enemyQuery, buffer, count, SpaceInvadersConstants.TypeEnemy);

        // Collect bullets
        count = CollectRenderEntities(bulletQuery, buffer, count, SpaceInvadersConstants.TypeBullet);

        runtimeService.SetRenderEntityCount(count);
    }

    private static int CollectRenderEntities(
        ComponentQuery<EntityId> query,
        RenderEntity[] buffer, int startIndex, uint entityType)
    {
        int count = startIndex;

        foreach (var entity in query)
        {
            if (count >= buffer.Length) return count;
            var pos = entity.Get<Position>();
            buffer[count] = new RenderEntity
            {
                Position = pos.Value,
                EntityType = entityType
            };
            count++;
        }

        return count;
    }
}
