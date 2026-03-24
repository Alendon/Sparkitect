using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Systems;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class GameplayGroup
{
    [EcsSystemFunction("bullet_cleanup")]
    [EcsSystemScheduling]
    [OrderAfter<GameplayGroup.CollisionFunc>]
    private static void BulletCleanupSystem(
        BulletCleanupQuery query,
        ICommandBufferAccessor commandBufferAccessor)
    {
        foreach (var entity in query)
        {
            var pos = entity.GetPosition();

            // Remove bullets that have left the screen (0-1 normalized space)
            if (pos.Value.Y < 0f || pos.Value.Y > 1f)
            {
                commandBufferAccessor.Modify<int>(entity.Key).DestroyEntity();
            }
        }
    }
}
