using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod;

public partial class GameplayGroup
{
    [EcsSystemFunction("enemy_ai")]
    [EcsSystemScheduling]
    private static void EnemyAiSystem(
        ComponentQuery<EntityId> query,
        ICommandBufferAccessor commandBufferAccessor,
        FrameTimingHolder frameTiming)
    {
        var dt = frameTiming.DeltaTime;
        var totalTime = frameTiming.TotalTime;

        // Per D-05/D-07: deterministic periodic horizontal velocity from sine wave
        var oscillationVelocity = MathF.Cos(totalTime * SpaceInvadersConstants.EnemyOscillationSpeed)
            * SpaceInvadersConstants.EnemyOscillationAmplitude
            * SpaceInvadersConstants.EnemyOscillationSpeed;

        EntityId shooterCandidate = EntityId.None;
        Vector2 shooterPosition = default;
        int eligibleCount = 0;

        foreach (var entity in query)
        {
            ref var vel = ref entity.GetRef<Velocity>();
            ref var cooldown = ref entity.GetRef<ShootCooldown>();
            var pos = entity.Get<Position>();

            vel.Value = new Vector2(oscillationVelocity, 0f);

            cooldown.Remaining -= dt;
            if (cooldown.Remaining <= 0f)
            {
                eligibleCount++;
                // Reservoir sampling: each eligible enemy has equal chance
                if (Random.Shared.Next(eligibleCount) == 0)
                {
                    shooterCandidate = entity.Key;
                    shooterPosition = pos.Value;
                }
            }
        }

        // Fire one random enemy bullet per frame (per D-08)
        if (shooterCandidate != EntityId.None)
        {
            // Reset shooter cooldown
            var modifyBuffer = commandBufferAccessor.Modify<int>(shooterCandidate);
            modifyBuffer.SetComponent(new ShootCooldown
            {
                Remaining = SpaceInvadersConstants.EnemyRefireCooldownMin +
                    Random.Shared.NextSingle() * (SpaceInvadersConstants.EnemyRefireCooldownMax - SpaceInvadersConstants.EnemyRefireCooldownMin)
            });

            // Spawn enemy bullet (per D-08: BulletData.Direction = -1)
            ICapabilityRequirement[] bulletFilter =
                [new ComponentSetRequirement([UnmanagedComponentID.SpaceInvadersMod.Position, UnmanagedComponentID.SpaceInvadersMod.Velocity, UnmanagedComponentID.SpaceInvadersMod.BulletData])];
            var bulletBuffer = commandBufferAccessor.Create<int>(bulletFilter);
            bulletBuffer.SetComponent(new Position { Value = new Vector2(shooterPosition.X, shooterPosition.Y + 0.02f) });
            bulletBuffer.SetComponent(new Velocity { Value = new Vector2(0f, SpaceInvadersConstants.BulletSpeed) });
            bulletBuffer.SetComponent(new BulletData { Direction = -1f });
        }
    }
}
