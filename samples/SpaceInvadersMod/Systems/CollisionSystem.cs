using System.Numerics;
using System.Runtime.CompilerServices;
using Sparkitect.ECS;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Systems;
using Sparkitect.Stateless;

namespace SpaceInvadersMod.Systems;

public partial class GameplayGroup
{

    [EcsSystemFunction("collision")]
    [EcsSystemScheduling]
    [OrderAfter<GameplayGroup.MovementFunc>]
    private static unsafe void CollisionSystem(
        BulletQuery bulletQuery, EnemyQuery enemyQuery, PlayerQuery playerQuery,
        ICommandBufferAccessor commandBufferAccessor)
    {
        foreach (var bullet in bulletQuery)
        {
            var playerBullet = bullet.GetBulletData().Direction > 0f;
            var bulletPos = bullet.GetPosition().Value;

            if (playerBullet)
            {
                foreach (var enemy in enemyQuery)
                {
                    var pos = enemy.GetPosition().Value;
                    if (!AabbOverlap(bulletPos, SpaceInvadersConstants.BulletHalfW, SpaceInvadersConstants.BulletHalfH,
                            pos, SpaceInvadersConstants.EnemyHalfW, SpaceInvadersConstants.EnemyHalfH)) continue;

                    commandBufferAccessor.Modify<int>(bullet.Key).DestroyEntity();
                    commandBufferAccessor.Modify<int>(enemy.Key).DestroyEntity();
                }
            }
            else
            {
                foreach (var player in playerQuery)
                {
                    var pos = player.GetPosition().Value;
                    if (!AabbOverlap(bulletPos, SpaceInvadersConstants.BulletHalfW, SpaceInvadersConstants.BulletHalfH,
                            pos, SpaceInvadersConstants.EnemyHalfW, SpaceInvadersConstants.EnemyHalfH)) continue;

                    commandBufferAccessor.Modify<int>(bullet.Key).DestroyEntity();
                    commandBufferAccessor.Modify<int>(player.Key).DestroyEntity();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AabbOverlap(Vector2 a, float aHalfW, float aHalfH, Vector2 b, float bHalfW, float bHalfH)
    {
        return MathF.Abs(a.X - b.X) < aHalfW + bHalfW &&
               MathF.Abs(a.Y - b.Y) < aHalfH + bHalfH;
    }
}
