using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class GameplayGroup
{
    [EcsSystemFunction("movement")]
    [EcsSystemScheduling]
    [OrderAfter<GameplayGroup.EnemyAiFunc>]
    private static void MovementSystem(
        ComponentQuery query,
        FrameTimingHolder frameTiming)
    {
        var dt = frameTiming.DeltaTime;

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetRef<Position>();
            var vel = entity.Get<Velocity>();

            pos.Value += vel.Value * dt;
        }
    }
}
