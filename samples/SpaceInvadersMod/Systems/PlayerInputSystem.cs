using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class SpaceInvadersSystemGroup
{
    [EcsSystemFunction("player_input")]
    [EcsSystemScheduling]
    [OrderBefore<GameplayGroup>]
    private static void PlayerInputSystem(
        ComponentQuery<EntityId> query,
        ICommandBufferAccessor commandBufferAccessor,
        FrameTimingHolder frameTiming,
        ISpaceInvadersRuntimeService runtimeService)
    {
        var dt = frameTiming.DeltaTime;

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetRef<Position>();
            ref var vel = ref entity.GetRef<Velocity>();
            ref var cooldown = ref entity.GetRef<ShootCooldown>();

            var moveDir = 0f;
            if (runtimeService.IsActionDown(GameAction.MoveLeft))
                moveDir -= 1f;
            if (runtimeService.IsActionDown(GameAction.MoveRight))
                moveDir += 1f;

            vel.Value = new Vector2(moveDir * SpaceInvadersConstants.PlayerSpeed, 0f);
            pos.Value.X = Math.Clamp(
                pos.Value.X + vel.Value.X * dt,
                SpaceInvadersConstants.PlayerHalfW,
                1f - SpaceInvadersConstants.PlayerHalfW);
            // Zero velocity after applying so MovementSystem doesn't double-apply
            vel.Value = Vector2.Zero;

            // Shooting (per SINV-02): fire bullet upward with cooldown
            cooldown.Remaining -= dt;
            if (cooldown.Remaining <= 0f && runtimeService.IsActionDown(GameAction.Shoot))
            {
                cooldown.Remaining = SpaceInvadersConstants.PlayerShootCooldown;

                // Create bullet via command buffer with filter matching bullet archetype
                ICapabilityRequirement[] bulletFilter =
                    [new ComponentSetRequirement([UnmanagedComponentID.SpaceInvadersMod.Position, UnmanagedComponentID.SpaceInvadersMod.Velocity, UnmanagedComponentID.SpaceInvadersMod.BulletData])];
                var bulletBuffer = commandBufferAccessor.Create<int>(bulletFilter);
                bulletBuffer.SetComponent(new Position { Value = new Vector2(pos.Value.X, pos.Value.Y - 0.02f) });
                bulletBuffer.SetComponent(new Velocity { Value = new Vector2(0f, -SpaceInvadersConstants.BulletSpeed) });
                bulletBuffer.SetComponent(new BulletData { Direction = 1f });
            }
        }
    }
}
