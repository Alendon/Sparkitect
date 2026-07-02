---
uid: sparkitect.ecs.command-buffers
title: Command Buffers
description: Deferring entity creation and destruction through session-based command buffers
---

# Command Buffers

Command buffers defer structural changes — creating entities, destroying them, setting components on a new entity — to a safe point outside system iteration. A system records intent; the recorded operations play back later, so iteration never mutates the storage it is walking.

Systems reach command buffers through the [`ICommandBufferAccessor`](xref:Sparkitect.ECS.Commands.ICommandBufferAccessor) parameter. One accessor is shared per world.

## Creating an Entity

Each buffer is a session scoped to a single entity. `Create` returns an [`ICommandBuffer`](xref:Sparkitect.ECS.Commands.ICommandBuffer) with an entity ID already allocated; record its components with `SetComponent`:

```csharp
ICapabilityRequirement[] bulletFilter =
[
    new ComponentSetRequirement([
        UnmanagedComponentID.SpaceInvadersMod.Position,
        UnmanagedComponentID.SpaceInvadersMod.Velocity,
        UnmanagedComponentID.SpaceInvadersMod.BulletData]),
];

var bullet = commandBufferAccessor.Create(bulletFilter);
bullet.SetComponent(new Position { Value = spawnPosition });
bullet.SetComponent(new Velocity { Value = new Vector2(0f, -BulletSpeed) });
bullet.SetComponent(new BulletData { Direction = 1f });
```

A [`ComponentSetRequirement`](xref:Sparkitect.ECS.Queries.ComponentSetRequirement) matches any storage whose component set is a superset of the listed IDs. `Create(filter)` resolves the filter and targets the first matching storage — the bullet archetype, here. When you already hold the target's handle, use the `Create(StorageHandle)` overload to skip resolution.

No entity handles cross the session boundary. Every `SetComponent` call applies to the one entity the session created, so the API stays free of per-call entity references.

## Destroying an Entity

Destroy an existing entity through a modify session. From inside a keyed query, the entity handle routes itself to the accessor:

```csharp
foreach (var entity in query)
{
    var pos = entity.GetPosition();
    if (pos.Value.Y < 0f || pos.Value.Y > 1f)
    {
        entity.Modify(commandBufferAccessor).DestroyEntity();
    }
}
```

`Modify(EntityId)` opens a session against an existing entity and fails fast if that entity is not in the `Bound` state. `DestroyEntity` records the removal; like every buffered operation, it takes effect only at playback.

## Playback

Recorded buffers do nothing until played back. The owner of the tick loop drives playback after systems run:

```csharp
var accessor = systemManager.GetCommandBufferAccessor(world);
accessor?.Playback();
```

`Playback` applies every recorded buffer in FIFO order, then clears the queue for the next frame. Running it between system executions is what makes deferral safe: structural changes land at a known point, never mid-iteration. The buffer does not prescribe when you call it — the execution model owns that timing.

## See Also

- <xref:sparkitect.ecs.queries> for the keyed queries whose handles issue destroy commands.
- <xref:sparkitect.ecs.worlds-storage> for the direct, non-deferred entity creation used during world setup.
- <xref:sparkitect.ecs.systems> for where playback fits in the tick loop.
