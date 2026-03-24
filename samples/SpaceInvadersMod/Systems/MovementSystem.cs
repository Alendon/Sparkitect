using Sparkitect.ECS.Systems;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class GameplayGroup
{
    [EcsSystemFunction("movement")]
    [EcsSystemScheduling]
    [OrderAfter<GameplayGroup.EnemyAiFunc>]
    private static void MovementSystem(
        MovementQuery query,
        FrameTimingHolder frameTiming)
    {
        var dt = frameTiming.DeltaTime;

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetPosition();
            var vel = entity.GetVelocity();

            pos.Value += vel.Value * dt;
        }
    }
}
