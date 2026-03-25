using Sparkitect.ECS.Systems;
using Sparkitect.Stateless;

namespace SpaceInvadersMod.Systems;

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
        foreach (var entity in playerQuery)
        {
            if (count >= buffer.Length) break;
            var pos = entity.GetPosition();
            buffer[count] = new RenderEntity
            {
                Position = pos.Value,
                EntityType = SpaceInvadersConstants.TypePlayer
            };
            count++;
        }

        // Collect enemies
        foreach (var entity in enemyQuery)
        {
            if (count >= buffer.Length) break;
            var pos = entity.GetPosition();
            buffer[count] = new RenderEntity
            {
                Position = pos.Value,
                EntityType = SpaceInvadersConstants.TypeEnemy
            };
            count++;
        }

        // Collect bullets
        foreach (var entity in bulletQuery)
        {
            if (count >= buffer.Length) break;
            var pos = entity.GetPosition();
            buffer[count] = new RenderEntity
            {
                Position = pos.Value,
                EntityType = SpaceInvadersConstants.TypeBullet
            };
            count++;
        }

        runtimeService.SetRenderEntityCount(count);
    }
}
